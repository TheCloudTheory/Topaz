---
slug: arm-deployment-cancel
title: "Cancelling ARM deployments in Topaz — what it means for an emulator"
authors: kamilmrzyglod
tags: [general, resource-manager]
---

Cancelling an in-progress deployment is one of those ARM operations that looks simple from the outside — you POST to a `/cancel` endpoint and the deployment stops. Under the hood, what "stop" means depends entirely on whether the engine executing the deployment can be interrupted mid-flight. In a real emulator, that question has a more nuanced answer than in the real Azure control plane.

This post walks through how Topaz implements `POST .../deployments/{name}/cancel`, what constraints the orchestrator model imposes, and where the emulation intentionally diverges from Azure's behaviour.

{/* truncate */}

## How Topaz executes deployments

Before looking at cancellation, it helps to understand how deployment execution works in Topaz.

When a client calls `PUT .../deployments/{name}`, the control plane parses the ARM template, persists a `DeploymentResource` to disk with `ProvisioningState: Created`, and enqueues a `TemplateDeployment` onto an in-memory queue. A single background thread — the `TemplateDeploymentOrchestrator` — drains that queue sequentially:

```csharp
OrchestratorThread = new Thread(() =>
{
    while (!stoppingToken.IsCancellationRequested)
    {
        TemplateDeployment? deployment = null;
        lock (QueueLock)
        {
            if (DeploymentQueue.Count > 0)
            {
                deployment = DeploymentQueue[0];
                DeploymentQueue.RemoveAt(0);
                _currentDeploymentId = deployment.Deployment.Id;
            }
        }

        if (deployment == null)
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
            continue;
        }

        RouteDeployment(deployment);
    }
});
```

`RouteDeployment` iterates over every resource in the template, dispatches each one to the right control plane (`Microsoft.KeyVault/vaults` → `KeyVaultControlPlane`, `Microsoft.Storage/storageAccounts` → `AzureStorageControlPlane`, etc.), and waits for each to complete before moving on. The whole thing is synchronous — one deployment at a time, one resource at a time within that deployment.

That sequential model is what makes cancellation straightforward in the common case, and impossible in others.

## The cancellation window

A deployment can only be cancelled while it is still sitting in the queue — that is, while its `ProvisioningState` is `Created` and the orchestrator thread has not yet picked it up.

The `CancelDeployment` method in `ResourceManagerControlPlane` enforces this explicitly:

```csharp
public OperationResult CancelDeployment(
    SubscriptionIdentifier subscriptionIdentifier,
    ResourceGroupIdentifier resourceGroupIdentifier,
    string deploymentName)
{
    var deploymentOp = GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
    if (deploymentOp.Result == OperationResult.NotFound)
        return OperationResult.NotFound;

    var provisioningState = deploymentOp.Resource!.Properties.ProvisioningState;
    if (provisioningState != ResourcesProvisioningState.Created.ToString())
        return OperationResult.Conflict;

    return templateDeploymentOrchestrator.CancelDeployment(
        subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
}
```

If the state check passes, the orchestrator takes over. It acquires the queue lock and checks whether the deployment has already been dequeued and set as `_currentDeploymentId` — meaning it is actively running:

```csharp
lock (QueueLock)
{
    if (_currentDeploymentId == deploymentId)
        return OperationResult.Conflict;

    toCancel = DeploymentQueue.FirstOrDefault(d => d.Deployment.Id == deploymentId);
    if (toCancel == null)
        return OperationResult.Conflict;

    DeploymentQueue.RemoveAll(d => d.Deployment.Id == deploymentId);
}

toCancel.Cancel();
provider.CreateOrUpdate(..., toCancel.Deployment);
return OperationResult.Success;
```

`Cancel()` transitions the `TemplateDeployment` to `DeploymentStatus.Cancelled` and calls `CancelDeployment()` on the persisted resource, which sets `ProvisioningState` to `Canceled` (the spelling Azure uses). The state is written back to disk immediately, so a subsequent GET on the deployment returns the updated status.

The endpoint itself maps the three possible outcomes to the appropriate HTTP responses:

```csharp
response.StatusCode = result switch
{
    OperationResult.Success  => HttpStatusCode.NoContent,   // 204
    OperationResult.NotFound => HttpStatusCode.NotFound,    // 404
    OperationResult.Conflict => HttpStatusCode.Conflict,    // 409
    _                        => HttpStatusCode.InternalServerError
};
```

This matches the real Azure REST API contract: `204` on success, `404` if the deployment does not exist, `409` if it cannot be cancelled.

## Where this diverges from real Azure

Real Azure can cancel a deployment that is already running — it stops provisioning further resources and leaves whatever was already created in place. The provisioningState transitions to `Canceled` at the deployment level even though some child resources may have been fully provisioned.

Topaz cannot do this. Once the orchestrator thread has dequeued a deployment and started calling `RouteDeployment`, there is no cooperative interruption mechanism. The thread runs each resource to completion before checking anything else. Cancelling a `Running` deployment returns `409 Conflict` — the same response Azure would return for a deployment that completed before the cancel request arrived.

The practical implication is that the cancellable window in Topaz is narrower than on Azure:

| State | Real Azure | Topaz |
|---|---|---|
| `Created` (queued) | Cancellable | Cancellable |
| `Running` (active) | Cancellable — stops mid-flight | **Not cancellable** — returns 409 |
| `Succeeded` / `Failed` | Not cancellable | Not cancellable |

For most test and automation scenarios this is not a meaningful gap. ARM templates in local development tend to be small and fast — deployments move from `Created` to `Succeeded` in milliseconds. The window where a cancel would reach a running deployment in real Azure is already narrow; in Topaz it simply does not exist.

## What this is useful for in practice

The more interesting use case in a local emulator is not cancelling a runaway deployment — it is testing that your tooling handles a `Canceled` provisioning state correctly.

Infrastructure-as-code pipelines, status polling loops, and portal equivalents all need to handle the full set of terminal states (`Succeeded`, `Failed`, `Canceled`). Without a way to drive a deployment into `Canceled`, that branch goes untested locally. The cancel endpoint makes it reachable:

```bash
# Submit a deployment
az deployment group create \
  --resource-group rg-dev \
  --template-file infra/main.bicep \
  --name deploy-01

# Cancel it before the orchestrator processes it
az deployment group cancel \
  --resource-group rg-dev \
  --name deploy-01

# Confirm the terminal state
az deployment group show \
  --resource-group rg-dev \
  --name deploy-01 \
  --query properties.provisioningState
# → "Canceled"
```

Because `ProvisioningState` is persisted to disk, the cancelled state survives across Topaz restarts, which means snapshot-based testing setups can pre-seed a cancelled deployment and drive their assertions without having to orchestrate the timing of a live cancel request.

## Takeaway

The cancel endpoint is a small but complete implementation: it matches the Azure REST contract for status codes and state transitions, enforces the correct preconditions with a lock-protected queue check, and persists the `Canceled` state durably. The one deliberate deviation — not supporting cancellation of actively running deployments — is a direct consequence of the synchronous orchestrator model and is worth knowing about when writing tests that depend on mid-flight cancellation semantics.

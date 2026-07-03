---
sidebar_position: 5
description: Topaz's design philosophy — what it emulates faithfully, where it simplifies, and why the code you write against Topaz runs unchanged against real Azure.
keywords: [topaz design, azure emulator design, topaz vs azure, local azure fidelity, topaz limitations, azure emulator philosophy]
---

# Service emulation design

Topaz's defining design goal is **API fidelity**: the Azure REST API calls your code makes against Topaz should work identically when pointed at real Azure. This page explains what that means in practice, where Topaz simplifies the real service behaviour, and the reasoning behind those choices.

## The fidelity goal

When you write a .NET application that stores a secret in Key Vault, the call goes through the Azure SDK:

```csharp
var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
await client.SetSecretAsync("my-secret", "hunter2");
```

Against Topaz, this call should succeed with the same response shape as it would against a real vault. Against real Azure, the same call — with only the URI changed — should also succeed. No code changes, no conditional compilation, no environment-specific branches.

This is the contract Topaz aims to maintain. It is different from mocking, where a test double returns hardcoded values that happen to pass assertions. Topaz implements the protocol, so the SDK's retry logic, error handling, content negotiation, and pagination all exercise real code paths.

## Where Topaz is complete

For supported services, Topaz implements:

- The full HTTP request and response shapes that the Azure SDK generates
- Authentication token validation on every call
- Resource lifecycle: create, read, update, delete
- Service-specific features: SAS tokens for Storage, soft-delete for Key Vault, AMQP sessions for Service Bus

The [Supported services](../supported-services.md) page lists the specific operations implemented for each service.

## What is already fully supported

**RBAC enforcement** — Role assignments are enforced at the data plane from the start. Assigning incorrect or missing roles produces the same `403 Forbidden` responses as real Azure.

**Durable storage** — Topaz persists state to disk by design. Resources, secrets, blobs, and messages survive `topaz-host` restarts.

**Cost estimation** — Topaz integrates with [Azure Cost Estimator (ACE)](../finops/overview.md) to provide local cost estimates for provisioned resources. See the FinOps overview for configuration details.

**Geo-replication and availability** — Multi-region and availability zone scenarios are within scope and actively being developed.

## What is not yet supported

**Subscription quotas and throughput throttling** — Topaz does not enforce per-subscription resource limits or service-level throttling. Resources that would be rejected in Azure for quota reasons will succeed locally.

**Network isolation and private endpoints** — Topaz currently listens on localhost without network-level isolation between resources or callers. Private endpoint emulation is under evaluation and will be added in a future release.

## Partial implementations

Some service operations are partially implemented — the most common operations work correctly, but less-used API paths may return a `501 Not Implemented` or an unexpected error. The [Supported services](../supported-services.md) page lists which operations are covered.

If you hit an unimplemented operation, the [GitHub issue tracker](https://github.com/TheCloudTheory/Topaz/issues) is the right place to request it or track its progress.

## The consequence for test design

Because Topaz implements real API semantics — including RBAC enforcement, durable state, and cost tracking — integration tests written against Topaz test the same code paths as production. The main gap is subscription quota and throttling behaviour, which Topaz does not replicate. For the logic of your application code and infrastructure declarations, Topaz is substantially equivalent to real Azure.

This is the trade-off Topaz is designed for: high-fidelity local testing without the cost and latency of a cloud subscription.

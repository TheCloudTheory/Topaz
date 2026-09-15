---
slug: finops-ace-cost-estimates
title: "FinOps for local Azure: cost estimates before you deploy"
description: Topaz integrates with ACE (Azure Cost Estimator) to estimate Azure costs against locally emulated resources, before anything touches a real subscription. Endpoint, CLI, and Portal workflow, with the actual integration code.
keywords: [azure cost estimation, finops, azure cost estimator, topaz finops, azure retail prices api, local azure cost feedback, iac cost estimation]
authors: kamilmrzyglod
tags: [topaz, finops, cost-estimation]
---

Where does the actual SKU decision happen for a typical Azure deployment? Not in the FinOps dashboard. It happens the moment someone writes `Standard_LRS` or `Premium_LRS` in a Bicep file, weeks before any dashboard has data to show. By the time a budget alert fires, the decision that caused it was already made, reviewed, and merged. If cost is one of the deciding factors when architecting a solution, it needs to be transparent and validated throughout the whole design and implementation phase, not just at the invoice stage.

{/* truncate */}

## The expectation: cost feedback should live next to the code

If you assume infrastructure cost is a property of the template, the reasonable expectation is that a cost estimate should be available at the same point you validate the template itself, before `terraform apply` or `az deployment` ever runs. That is roughly what [ACE (Azure Cost Estimator)](https://github.com/thecloudtheory/arm-estimator) does on its own: it runs a `what-if` analysis against your Bicep, ARM or Terraform template, resolves the resulting resource changes against the [Azure Retail Prices API](https://prices.azure.com/api/retail/prices), and returns a cost breakdown per resource.

The gap is that `what-if` still needs a real target. ACE on its own gets you cost feedback earlier than a monthly invoice, but not earlier than "you need a subscription to run this against." That is the part Topaz closes.

## Building the playground

Topaz provisions resources against the emulator instead of Azure, and it writes each one to disk as `.topaz` state as it goes. That state is exactly the resource inventory ACE needs to produce an estimate for, already sitting there locally, with no subscription required to generate it. The integration wires ACE in as a compiled library, not a subprocess call:

```xml
<!-- Topaz.FinOps.csproj -->
<PackageReference Include="TheCloudTheory.AzureCostEstimator.Core" Version="1.9.0" />
```

Three pieces do the actual work:

- `ResourceInventoryCollector` walks the emulator's `.topaz` state directory and collects the ARM resource JSON snapshots for a subscription. This is the "what actually got deployed" side.
- `CostEstimationService` bridges that inventory with ACE's `EstimationService`, using an `ArmClient` scoped to Topaz's local credential and endpoint configuration instead of a real Azure tenant:

```csharp
var armClient = new ArmClient(
    new AzureLocalCredential(Globals.GlobalAdminId),
    subscriptionId,
    TopazArmClientOptions.New);

var options = new CoreEstimationOptions
{
    Currency = currencyCode,
    ArmClient = armClient
};

var output = await AceEstimationService.EstimateAsync(changes, options, null, cancellationToken);
```

- `GetEstimatedCostsEndpoint` exposes the result over HTTP. The CLI and the Portal both consume that same endpoint rather than duplicating estimation logic.

The interesting detail is that ACE never has to know it isn't talking to real Azure. `ArmClient` is the same abstraction either way, so the estimation code path that runs against a live subscription runs unmodified against the emulator.

## Walking through the evidence

The endpoint:

```
GET /topaz/subscriptions/{subscriptionId}/estimatedCosts?currency=USD
```

`currency` is optional and defaults to `USD` (ACE supports 17 different currency codes). Response shape:

```json
{
  "subscriptionId": "f1a2b3c4-d5e6-7890-abcd-ef0011223344",
  "currency": "USD",
  "totalMonthlyCost": 142.50,
  "resources": [
    {
      "resourceId": "/subscriptions/.../providers/Microsoft.KeyVault/vaults/my-vault",
      "resourceType": "Microsoft.KeyVault/vaults",
      "estimatedMonthlyCost": 12.00
    }
  ]
}
```

One thing worth calling out explicitly: resource types without a matching ACE estimator are still listed in the response, just with `estimatedMonthlyCost` at `0.00`, instead of being silently dropped. That matters if you plan to treat the total as anything more than a sanity check.

From the CLI:

```bash
topaz finops estimate --subscription f1a2b3c4-d5e6-7890-abcd-ef0011223344 --currency EUR --output json
```

Or the default table:

```
┌─────────────────────────────────────────────────┬──────────────────────────┐
│ Resource type                                   │ Estimated monthly cost   │
├─────────────────────────────────────────────────┼──────────────────────────┤
│ Microsoft.KeyVault/vaults                       │ $12.00                   │
│ Microsoft.Storage/storageAccounts               │ $4.80                    │
│ Microsoft.ContainerRegistry/registries          │ $5.00                    │
├─────────────────────────────────────────────────┼──────────────────────────┤
│ Total                                           │ $21.80 / month           │
└─────────────────────────────────────────────────┴──────────────────────────┘
```

The [Portal's](/docs/portal) `/cost-analysis` page shows the same numbers with a subscription and currency selector, and a `CostSummaryWidget` surfaces the running total on the subscription overview page, so the number is visible without navigating anywhere specific.

## What this means in practice

The practical use isn't a one-off number. It's running `topaz finops estimate`, or hitting the endpoint from CI, after every deployment against the emulator, and diffing the total against the previous run. Because nothing here touches a real subscription, this can run on every PR without waiting on `what-if` against Azure or paying for a dev environment to test a SKU change. A change that quietly adds $40/month to the estimate shows up in the same feedback loop as a failed test, not three weeks later on an invoice.

## Try it

```bash
topaz finops estimate --subscription <subscriptionId>
```

Full endpoint and CLI reference is in [FinOps overview](/docs/finops/overview).

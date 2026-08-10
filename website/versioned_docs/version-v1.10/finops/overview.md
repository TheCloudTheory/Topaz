---
sidebar_position: 1
description: Learn how Topaz integrates with ACE (Azure Cost Estimator) to provide monthly cost estimates for your locally emulated Azure resources.
keywords: [topaz finops, azure cost estimator, ace, cost estimation, local azure cost, finops]
---

# FinOps overview

Topaz integrates with [ACE (Azure Cost Estimator)](https://github.com/TheCloudTheory/arm-estimator) to give you monthly cost estimates for all resources running inside your local emulator. This lets you catch expensive configurations early — before deploying to real Azure.

## How it works

When you call the cost estimation endpoint, Topaz collects all resources provisioned in the requested subscription, queries the [Azure Retail Prices API](https://learn.microsoft.com/en-us/rest/api/cost-management/retail-prices/azure-retail-prices) via ACE, and returns a JSON response with per-resource monthly cost breakdowns.

:::note
Cost estimates are based on list prices from the Azure Retail Prices API. They do not account for reservations, committed-use discounts, or enterprise agreements.
:::

## REST endpoint

```
GET https://topaz.local.dev:8899/topaz/subscriptions/{subscriptionId}/estimatedCosts
```

### Query parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `currency` | string | `USD` | ISO 4217 currency code. ACE supports 17 currencies: `USD`, `EUR`, `GBP`, `AUD`, `BRL`, `CAD`, `CHF`, `CNY`, `DKK`, `INR`, `JPY`, `KRW`, `NOK`, `NZD`, `RUB`, `SEK`, `TWD`. |

### Response shape

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

### Example — curl

```bash
curl -s \
  "https://topaz.local.dev:8899/topaz/subscriptions/$(az account show --query id -o tsv)/estimatedCosts?currency=EUR" \
  --cacert topaz.crt | jq .
```

## CLI command

The `topaz finops estimate` command queries the endpoint and renders the result as a table:

```bash
topaz finops estimate --subscription <subscriptionId>
```

### Options

| Flag | Default | Description |
|------|---------|-------------|
| `--subscription` | _(required)_ | Target subscription ID. |
| `--currency` | `USD` | Currency code (see supported currencies above). |
| `--output` | `table` | Output format: `table` or `json`. |

### Example output

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

## Supported resource types

ACE covers a wide range of Azure resource types. Resources whose type is not recognised by ACE are counted but not priced (they appear with `$0.00`). See the [ACE documentation](https://github.com/TheCloudTheory/arm-estimator) for the full list of supported services.

## Known limitations

- The endpoint calls the live Azure Retail Prices API. It requires outbound internet access from the machine running Topaz.
- Resources with no `location` set are skipped by the ACE engine.

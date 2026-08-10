---
sidebar_position: 11
---

# cosmosdb account list-keys
Lists the access keys for an Azure Cosmos DB account.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Cosmos DB account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List keys for a Cosmos DB account
```bash
$ topaz cosmosdb account list-keys \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-cosmos-account"
```

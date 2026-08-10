---
sidebar_position: 1
---

# cosmosdb account create
Creates or updates an Azure Cosmos DB account.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Cosmos DB account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `--kind` - (Optional) Account kind: GlobalDocumentDB, MongoDB, Parse. Defaults to GlobalDocumentDB.

## Examples

### Create a Cosmos DB account
```bash
$ topaz cosmosdb account create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-cosmos-account" \
    --location "westeurope"
```

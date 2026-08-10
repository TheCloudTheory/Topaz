---
sidebar_position: 3
---

# cosmosdb account delete
Deletes an Azure Cosmos DB account.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Cosmos DB account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete a Cosmos DB account
```bash
$ topaz cosmosdb account delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-cosmos-account"
```

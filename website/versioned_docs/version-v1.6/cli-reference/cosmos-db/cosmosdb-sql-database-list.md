---
sidebar_position: 13
---

# cosmosdb sql-database list
Lists SQL databases in an Azure Cosmos DB account.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-a, --account-name` - (Required) (Required) Cosmos DB account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List SQL databases
```bash
$ topaz cosmosdb sql-database list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "my-cosmos-account"
```

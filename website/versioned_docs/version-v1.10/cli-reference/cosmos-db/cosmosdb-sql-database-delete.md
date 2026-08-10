---
sidebar_position: 4
---

# cosmosdb sql-database delete
Deletes a SQL database in an Azure Cosmos DB account.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-a, --account-name` - (Required) (Required) Cosmos DB account name.
* `-n, --database-name` - (Required) (Required) SQL database name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete a SQL database
```bash
$ topaz cosmosdb sql-database delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "my-cosmos-account" \
    --database-name "my-database"
```

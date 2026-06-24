---
sidebar_position: 2
---

# sql db delete
Deletes an Azure SQL Database.

## Options
* `--server` - (Required) (Required) SQL server name.
* `-n, --name` - (Required) (Required) SQL database name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Deletes a SQL Database
```bash
$ topaz sql db delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --server "my-sql-server" \
    --name "my-database" \
    --resource-group "rg-local"
```

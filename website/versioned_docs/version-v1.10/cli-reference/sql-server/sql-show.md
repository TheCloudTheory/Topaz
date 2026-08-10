---
sidebar_position: 3
---

# sql show
Gets an Azure SQL Server.

## Options
* `-n, --name` - (Required) (Required) SQL server name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Gets a SQL Server
```bash
$ topaz sql show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-sql-server" \
    --resource-group "rg-local"
```

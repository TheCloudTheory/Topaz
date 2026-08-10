---
sidebar_position: 1
---

# sql create
Creates or updates an Azure SQL Server.

## Options
* `-n, --name` - (Required) (Required) SQL server name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-u, --admin-user` - (Required) (Required) Administrator login username.
* `-p, --admin-password` - (Required) (Required) Administrator login password.

## Examples

### Creates a new SQL Server
```bash
$ topaz sql create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-sql-server" \
    --location "westeurope" \
    --resource-group "rg-local" \
    --admin-user "sqladmin" \
    --admin-password "SqlAdmin1234!@#"
```

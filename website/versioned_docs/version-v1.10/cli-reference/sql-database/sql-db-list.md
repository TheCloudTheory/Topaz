---
sidebar_position: 4
---

# sql db list
Lists Azure SQL Databases under a server.

## Options
* `--server` - (Required) (Required) SQL server name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Lists SQL Databases in a server
```bash
$ topaz sql db list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --server "my-sql-server" \
    --resource-group "rg-local"
```

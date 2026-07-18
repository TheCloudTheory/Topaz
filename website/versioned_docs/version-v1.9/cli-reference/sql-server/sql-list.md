---
sidebar_position: 4
---

# sql list
Lists Azure SQL Servers in a subscription or resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - (Optional) Filter by resource group name.

## Examples

### Lists SQL Servers in a resource group
```bash
$ topaz sql list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

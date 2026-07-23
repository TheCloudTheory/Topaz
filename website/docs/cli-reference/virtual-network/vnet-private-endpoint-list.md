---
sidebar_position: 13
---

# vnet private-endpoint list
Lists private endpoints in a resource group.

## Options
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Lists all private endpoints in a resource group
```bash
$ topaz vnet private-endpoint list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

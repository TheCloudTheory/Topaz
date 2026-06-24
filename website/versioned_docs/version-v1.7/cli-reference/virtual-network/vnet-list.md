---
sidebar_position: 5
---

# vnet list
Lists Azure Virtual Networks in a subscription or resource group.

## Options
* `-s, --subscription-id` - (Required) subscription ID
* `-g, --resource-group` - (Optional) filter by resource group name

## Examples

### Lists Virtual Networks in a resource group
```bash
$ topaz vnet list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

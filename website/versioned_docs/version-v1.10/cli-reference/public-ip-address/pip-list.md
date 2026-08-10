---
sidebar_position: 4
---

# pip list
Lists Azure Public IP Addresses in a subscription or resource group.

## Options
* `-s, --subscription-id` - (Required) subscription ID
* `-g, --resource-group` - (Optional) filter by resource group name

## Examples

### Lists Public IP Addresses in a resource group
```bash
$ topaz pip list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

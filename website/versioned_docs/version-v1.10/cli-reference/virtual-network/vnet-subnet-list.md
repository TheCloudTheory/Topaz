---
sidebar_position: 9
---

# vnet subnet list
Lists subnets in a Virtual Network.

## Options
* `--vnet-name` - (Required) virtual network name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Lists all subnets
```bash
$ topaz vnet subnet list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --vnet-name "my-vnet" \
    --resource-group "rg-local"
```

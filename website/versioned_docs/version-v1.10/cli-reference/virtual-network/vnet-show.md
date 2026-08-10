---
sidebar_position: 4
---

# vnet show
Gets an Azure Virtual Network.

## Options
* `-n, --name` - (Required) virtual network name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets a Virtual Network
```bash
$ topaz vnet show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-vnet" \
    --resource-group "rg-local"
```

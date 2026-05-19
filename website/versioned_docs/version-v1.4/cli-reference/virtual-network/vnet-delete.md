---
sidebar_position: 3
---

# vnet delete
Deletes an Azure Virtual Network.

## Options
* `-n, --name` - (Required) virtual network name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes a Virtual Network
```bash
$ topaz vnet delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-vnet" \
    --resource-group "rg-local"
```

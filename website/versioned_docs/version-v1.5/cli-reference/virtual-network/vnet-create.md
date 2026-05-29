---
sidebar_position: 2
---

# vnet create
Creates or updates an Azure Virtual Network.

## Options
* `-n, --name` - (Required) virtual network name
* `-g, --resource-group` - (Required) resource group name
* `-l, --location` - (Required) location
* `-s, --subscription-id` - (Required) subscription ID
* `--address-prefix` - Address prefix for the virtual network (e.g. 10.0.0.0/16)

## Examples

### Creates a new Virtual Network
```bash
$ topaz vnet create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-vnet" \
    --location "westeurope" \
    --resource-group "rg-local" \
    --address-prefix "10.0.0.0/16"
```

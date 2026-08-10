---
sidebar_position: 1
---

# vnet check-ip
Checks whether a private IP address is available for use in a Virtual Network.

## Options
* `-n, --name` - (Required) virtual network name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID
* `-i, --ip-address` - (Required) private IP address to check

## Examples

### Check IP address availability
```bash
$ topaz vnet check-ip --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-vnet" \
    --resource-group "rg-local" \
    --ip-address "10.0.1.5"
```

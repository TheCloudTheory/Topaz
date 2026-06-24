---
sidebar_position: 6
---

# vnet subnet create
Creates or updates a subnet in a Virtual Network.

## Options
* `-n, --name` - (Required) subnet name
* `--vnet-name` - (Required) virtual network name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID
* `--address-prefix` - Address prefix for the subnet (e.g. 10.0.1.0/24)

## Examples

### Creates a subnet
```bash
$ topaz vnet subnet create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --vnet-name "my-vnet" \
    --name "my-subnet" \
    --address-prefix "10.0.1.0/24" \
    --resource-group "rg-local"
```

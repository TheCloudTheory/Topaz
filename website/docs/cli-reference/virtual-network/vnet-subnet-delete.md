---
sidebar_position: 7
---

# vnet subnet delete
Deletes a subnet from a Virtual Network.

## Options
* `-n, --name` - (Required) subnet name
* `--vnet-name` - (Required) virtual network name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes a subnet
```bash
$ topaz vnet subnet delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --vnet-name "my-vnet" \
    --name "my-subnet" \
    --resource-group "rg-local"
```

---
sidebar_position: 1
---

# pip create
Creates or updates an Azure Public IP Address.

## Options
* `-n, --name` - (Required) public IP address name
* `-g, --resource-group` - (Required) resource group name
* `-l, --location` - (Required) location
* `-s, --subscription-id` - (Required) subscription ID
* `--allocation-method` - IP allocation method (Dynamic or Static, default: Dynamic)
* `--version` - IP address version (IPv4 or IPv6, default: IPv4)

## Examples

### Creates a new Public IP Address
```bash
$ topaz pip create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-pip" \
    --location "westeurope" \
    --resource-group "rg-local"
```

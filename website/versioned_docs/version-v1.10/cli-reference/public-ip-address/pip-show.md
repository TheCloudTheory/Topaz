---
sidebar_position: 3
---

# pip show
Gets an Azure Public IP Address.

## Options
* `-n, --name` - (Required) public IP address name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets a Public IP Address
```bash
$ topaz pip show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-pip" \
    --resource-group "rg-local"
```

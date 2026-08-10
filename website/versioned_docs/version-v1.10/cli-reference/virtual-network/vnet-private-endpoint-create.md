---
sidebar_position: 10
---

# vnet private-endpoint create
Creates or updates a private endpoint.

## Options
* `-n, --name` - (Required) private endpoint name
* `-l, --location` - (Required) location
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Creates a private endpoint
```bash
$ topaz vnet private-endpoint create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-pe" \
    --location "eastus" \
    --resource-group "rg-local"
```

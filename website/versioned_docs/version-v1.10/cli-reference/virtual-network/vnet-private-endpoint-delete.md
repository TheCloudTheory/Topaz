---
sidebar_position: 11
---

# vnet private-endpoint delete
Deletes a private endpoint.

## Options
* `-n, --name` - (Required) private endpoint name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes a private endpoint
```bash
$ topaz vnet private-endpoint delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-pe" \
    --resource-group "rg-local"
```

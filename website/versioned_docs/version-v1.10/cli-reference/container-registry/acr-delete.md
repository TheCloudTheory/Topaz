---
sidebar_position: 3
---

# acr delete
Deletes an Azure Container Registry.

## Options
* `-n, --name` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.

## Examples

### Delete a registry
```bash
$ topaz acr delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry"
```

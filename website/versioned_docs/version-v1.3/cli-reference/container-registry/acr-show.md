---
sidebar_position: 11
---

# acr show
Shows details of an Azure Container Registry.

## Options
* `-n, --name` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.

## Examples

### Show a registry
```bash
$ topaz acr show \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry"
```

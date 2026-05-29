---
sidebar_position: 8
---

# acr list-usages
Lists quota usages for an Azure Container Registry.

## Options
* `-n, --name` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.

## Examples

### List usages for a registry
```bash
$ topaz acr list-usages \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry"
```

---
sidebar_position: 7
---

# acr list-credentials
Lists admin credentials for an Azure Container Registry.

## Options
* `-n, --name` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.

## Examples

### List credentials for a registry
```bash
$ topaz acr list-credentials \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry"
```

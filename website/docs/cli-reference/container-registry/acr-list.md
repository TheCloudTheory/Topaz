---
sidebar_position: 6
---

# acr list
Lists Azure Container Registries.

## Options
* `-s, --subscription-id` - (Required) Subscription ID.
* `-g, --resource-group` - Resource group name. Omit to list across the entire subscription.

## Examples

### List registries in a resource group
```bash
$ topaz acr list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg"
```

### List all registries in a subscription
```bash
$ topaz acr list \
    --subscription-id "00000000-0000-0000-0000-000000000000"
```

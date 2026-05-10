---
sidebar_position: 1
---

# servicebus namespace create
Creates or updates a Service Bus namespace.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Create a namespace
```bash
$ topaz servicebus namespace create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "sblocal"
```

---
sidebar_position: 3
---

# servicebus namespace delete
Deletes a Service Bus namespace.

## Options
* `-n, --name` - (Required) (Required) Namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Delete a namespace
```bash
$ topaz servicebus namespace delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "sblocal"
```

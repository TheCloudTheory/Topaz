---
sidebar_position: 11
---

# eventgrid namespace delete
Deletes an Event Grid Namespace.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete an Event Grid Namespace
```bash
$ topaz eventgrid namespace delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-namespace"
```

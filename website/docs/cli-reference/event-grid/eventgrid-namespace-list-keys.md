---
sidebar_position: 6
---

# eventgrid namespace list-keys
Lists access keys for an Event Grid Namespace.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List keys for an Event Grid Namespace
```bash
$ topaz eventgrid namespace list-keys \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-namespace"
```

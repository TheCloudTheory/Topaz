---
sidebar_position: 7
---

# eventgrid topic list-keys
Lists access keys for an Event Grid Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List keys for an Event Grid Topic
```bash
$ topaz eventgrid topic list-keys \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-topic"
```

---
sidebar_position: 19
---

# eventgrid system-topic show
Gets an Event Grid System Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid System Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Get an Event Grid System Topic
```bash
$ topaz eventgrid system-topic show \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-system-topic"
```

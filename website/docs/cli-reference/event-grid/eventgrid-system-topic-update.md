---
sidebar_position: 22
---

# eventgrid system-topic update
Updates an Event Grid System Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid System Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Update an Event Grid System Topic
```bash
$ topaz eventgrid system-topic update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-system-topic" \
    --tags "env=prod"
```

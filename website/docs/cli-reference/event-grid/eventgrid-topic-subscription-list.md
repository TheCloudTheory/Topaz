---
sidebar_position: 6
---

# eventgrid topic subscription list
Lists event subscriptions for an Event Grid Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-t, --topic-name` - (Required) (Required) Event Grid Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List event subscriptions for an Event Grid Topic
```bash
$ topaz eventgrid topic subscription list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --topic-name "my-topic"
```

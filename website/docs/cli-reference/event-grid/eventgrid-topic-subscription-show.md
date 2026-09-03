---
sidebar_position: 3
---

# eventgrid topic subscription show
Gets an Event Grid Topic event subscription.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-t, --topic-name` - (Required) (Required) Event Grid Topic name.
* `-n, --name` - (Required) (Required) Event subscription name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Get an Event Grid Topic event subscription
```bash
$ topaz eventgrid topic subscription show \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --topic-name "my-topic" \
    --name "my-subscription"
```

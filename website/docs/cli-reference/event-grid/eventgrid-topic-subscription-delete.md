---
sidebar_position: 2
---

# eventgrid topic subscription delete
Deletes an Event Grid Topic event subscription.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-t, --topic-name` - (Required) (Required) Event Grid Topic name.
* `-n, --name` - (Required) (Required) Event subscription name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete an Event Grid Topic event subscription
```bash
$ topaz eventgrid topic subscription delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --topic-name "my-topic" \
    --name "my-subscription"
```

---
sidebar_position: 7
---

# eventgrid topic subscription update
Updates an Event Grid Topic event subscription.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-t, --topic-name` - (Required) (Required) Event Grid Topic name.
* `-n, --name` - (Required) (Required) Event subscription name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `--endpoint-url` - (Optional) Webhook endpoint URL to deliver events to.

## Examples

### Update an Event Grid Topic event subscription
```bash
$ topaz eventgrid topic subscription update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --topic-name "my-topic" \
    --name "my-subscription" \
    --endpoint-url "https://example.com/webhook"
```

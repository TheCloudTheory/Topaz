---
sidebar_position: 1
---

# eventgrid topic subscription create
Creates or updates an Event Grid Topic event subscription.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-t, --topic-name` - (Required) (Required) Event Grid Topic name.
* `-n, --name` - (Required) (Required) Event subscription name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `--endpoint-url` - (Required) (Required) Webhook endpoint URL to deliver events to.

## Examples

### Create an Event Grid Topic event subscription
```bash
$ topaz eventgrid topic subscription create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --topic-name "my-topic" \
    --name "my-subscription" \
    --endpoint-url "https://example.com/webhook"
```

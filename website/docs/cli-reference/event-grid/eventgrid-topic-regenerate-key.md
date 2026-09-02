---
sidebar_position: 8
---

# eventgrid topic regenerate-key
Regenerates an access key for an Event Grid Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-k, --key-name` - (Required) (Required) Key name to regenerate: key1 or key2.

## Examples

### Regenerate the primary key for an Event Grid Topic
```bash
$ topaz eventgrid topic regenerate-key \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-topic" \
    --key-name "key1"
```

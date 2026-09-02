---
sidebar_position: 9
---

# eventgrid topic update
Updates an Event Grid Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `--public-network-access` - (Optional) Public network access: Enabled or Disabled.

## Examples

### Update an Event Grid Topic
```bash
$ topaz eventgrid topic update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-topic" \
    --public-network-access "Disabled"
```

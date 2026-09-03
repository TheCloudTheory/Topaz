---
sidebar_position: 8
---

# eventgrid topic create
Creates or updates an Event Grid Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.

## Examples

### Create an Event Grid Topic
```bash
$ topaz eventgrid topic create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-topic" \
    --location "westeurope"
```

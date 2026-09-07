---
sidebar_position: 17
---

# eventgrid system-topic create
Creates or updates an Event Grid System Topic.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid System Topic name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `--source` - (Optional) Resource ID of the source of events.
* `--topic-type` - (Optional) Topic type of the source.

## Examples

### Create an Event Grid System Topic
```bash
$ topaz eventgrid system-topic create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-system-topic" \
    --location "westeurope" \
    --source "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-local/providers/Microsoft.Storage/storageAccounts/mystorage" \
    --topic-type "Microsoft.Storage.StorageAccounts"
```

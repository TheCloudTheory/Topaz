---
sidebar_position: 9
---

# availability-set delete
Deletes an Azure Availability Set.

## Options
* `-n, --name` - (Required) (Required) Availability set name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Deletes an Availability Set
```bash
$ topaz availability-set delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-avset" \
    --resource-group "rg-local"
```

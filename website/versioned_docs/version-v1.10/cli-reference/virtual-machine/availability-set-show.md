---
sidebar_position: 10
---

# availability-set show
Gets an Azure Availability Set.

## Options
* `-n, --name` - (Required) (Required) Availability set name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Gets an Availability Set
```bash
$ topaz availability-set show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-avset" \
    --resource-group "rg-local"
```

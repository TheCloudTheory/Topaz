---
sidebar_position: 13
---

# availability-set list-available-sizes
Lists available VM sizes for an Azure Availability Set.

## Options
* `-n, --name` - (Required) (Required) Availability set name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Lists available VM sizes for an Availability Set
```bash
$ topaz availability-set list-available-sizes --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-avset" \
    --resource-group "rg-local"
```

---
sidebar_position: 8
---

# availability-set create
Creates or updates an Azure Availability Set.

## Options
* `-n, --name` - (Required) (Required) Availability set name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--fault-domain-count` - (Optional) Number of fault domains.
* `--update-domain-count` - (Optional) Number of update domains.

## Examples

### Creates an Availability Set
```bash
$ topaz availability-set create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-avset" \
    --resource-group "rg-local" \
    --location "westeurope"
```

---
sidebar_position: 14
---

# availability-set update
Updates an Azure Availability Set.

## Options
* `-n, --name` - (Required) (Required) Availability set name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--fault-domain-count` - (Optional) Number of fault domains.
* `--update-domain-count` - (Optional) Number of update domains.

## Examples

### Updates an Availability Set
```bash
$ topaz availability-set update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-avset" \
    --resource-group "rg-local" \
    --fault-domain-count 3
```

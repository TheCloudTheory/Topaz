---
sidebar_position: 12
---

# availability-set list
Lists Azure Availability Sets in a resource group.

## Options
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Lists Availability Sets in a resource group
```bash
$ topaz availability-set list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

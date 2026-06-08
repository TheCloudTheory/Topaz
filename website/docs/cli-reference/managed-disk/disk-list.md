---
sidebar_position: 4
---

# disk list
Lists Azure Managed Disks in a subscription or resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - (Optional) Filter by resource group name.

## Examples

### Lists Managed Disks in a resource group
```bash
$ topaz disk list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

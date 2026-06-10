---
sidebar_position: 6
---

# disk revoke-access
Revokes SAS access from an Azure Managed Disk.

## Options
* `-n, --name` - (Required) (Required) Disk name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Revoke SAS access from a Managed Disk
```bash
$ topaz disk revoke-access --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-disk" \
    --resource-group "rg-local"
```

---
sidebar_position: 3
---

# disk show
Gets an Azure Managed Disk.

## Options
* `-n, --name` - (Required) (Required) Disk name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Gets a Managed Disk
```bash
$ topaz disk show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-disk" \
    --resource-group "rg-local"
```

---
sidebar_position: 2
---

# disk delete
Deletes an Azure Managed Disk.

## Options
* `-n, --name` - (Required) (Required) Disk name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Deletes a Managed Disk
```bash
$ topaz disk delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-disk" \
    --resource-group "rg-local"
```

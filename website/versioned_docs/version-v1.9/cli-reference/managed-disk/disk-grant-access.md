---
sidebar_position: 4
---

# disk grant-access
Grants SAS access to an Azure Managed Disk.

## Options
* `-n, --name` - (Required) (Required) Disk name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--access` - (Required) (Required) Access level: Read, Write, or ReadWrite.
* `--duration-in-seconds` - (Required) (Required) Duration of SAS access in seconds.

## Examples

### Grant read access to a Managed Disk
```bash
$ topaz disk grant-access --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-disk" \
    --resource-group "rg-local" \
    --access "Read" \
    --duration-in-seconds 3600
```

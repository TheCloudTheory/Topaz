---
sidebar_position: 5
---

# disk update
Updates an Azure Managed Disk (tags, diskSizeGB, SKU).

## Options
* `-n, --name` - (Required) (Required) Disk name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--tags` - (Optional) Space-separated tags in key=value format.
* `--sku` - (Optional) SKU name (e.g. Premium_LRS, Standard_LRS).
* `--disk-size-gb` - (Optional) New disk size in GB.

## Examples

### Updates a Managed Disk's tags
```bash
$ topaz disk update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-disk" \
    --resource-group "rg-local" \
    --tags env=test
```

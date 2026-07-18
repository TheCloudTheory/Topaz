---
sidebar_position: 1
---

# disk create
Creates or updates an Azure Managed Disk.

## Options
* `-n, --name` - (Required) (Required) Disk name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--disk-size-gb` - (Optional) Disk size in GB. Defaults to 32.
* `--sku` - (Optional) SKU name (e.g. Premium_LRS, Standard_LRS).

## Examples

### Creates a new Managed Disk
```bash
$ topaz disk create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-disk" \
    --location "westeurope" \
    --resource-group "rg-local"
```

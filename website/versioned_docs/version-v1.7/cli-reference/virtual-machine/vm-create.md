---
sidebar_position: 1
---

# vm create
Creates or updates an Azure Virtual Machine.

## Options
* `-n, --name` - (Required) (Required) Virtual machine name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--size` - (Optional) VM size (e.g. Standard_D2_v3). Defaults to Standard_D2_v3.

## Examples

### Creates a new Virtual Machine
```bash
$ topaz vm create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-vm" \
    --location "westeurope" \
    --resource-group "rg-local" \
    --size "Standard_D2_v3"
```

---
sidebar_position: 7
---

# vm update
Updates an Azure Virtual Machine.

## Options
* `-n, --name` - (Required) (Required) Virtual machine name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--size` - (Optional) Virtual machine size.

## Examples

### Updates a Virtual Machine
```bash
$ topaz vm update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-vm" \
    --resource-group "rg-local" \
    --size "Standard_D2_v3"
```

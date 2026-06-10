---
sidebar_position: 3
---

# vm show
Gets an Azure Virtual Machine.

## Options
* `-n, --name` - (Required) (Required) Virtual machine name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Gets a Virtual Machine
```bash
$ topaz vm show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-vm" \
    --resource-group "rg-local"
```

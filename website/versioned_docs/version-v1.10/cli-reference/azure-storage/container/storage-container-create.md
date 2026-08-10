---
sidebar_position: 1
---

# storage container create
Creates a new blob container in a storage account.

## Options
* `-n, --name` - (Required) (Required) Container name.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Create a container
```bash
$ topaz storage container create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --name "mycontainer"
```

---
sidebar_position: 2
---

# storage blob delete
Deletes a blob from a container.

## Options
* `--account-name` - (Required) (Required) Storage account name.
* `-c, --container-name` - (Required) (Required) Container name.
* `-n, --name` - (Required) (Required) Blob name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Delete a blob
```bash
$ topaz storage blob delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --container-name "mycontainer" \
    --name "file.txt"
```

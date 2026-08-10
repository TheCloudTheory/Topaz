---
sidebar_position: 3
---

# storage blob download
Downloads a blob to a local file.

## Options
* `--account-name` - (Required) (Required) Storage account name.
* `-c, --container-name` - (Required) (Required) Container name.
* `-n, --name` - (Required) (Required) Blob name.
* `-d, --destination` - Destination file path (defaults to blob name).
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Download a blob
```bash
$ topaz storage blob download \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --container-name "mycontainer" \
    --name "file.txt" \
    --destination "/tmp/file.txt"
```

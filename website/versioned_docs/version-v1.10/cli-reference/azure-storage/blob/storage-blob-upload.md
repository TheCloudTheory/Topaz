---
sidebar_position: 12
---

# storage blob upload
Uploads a local file to a blob container.

## Options
* `--account-name` - (Required) (Required) Storage account name.
* `-c, --container-name` - (Required) (Required) Container name.
* `-f, --file` - (Required) (Required) Path to the local file to upload.
* `-n, --name` - Blob name (defaults to the file name).
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Upload a file
```bash
$ topaz storage blob upload \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --container-name "mycontainer" \
    --file "/path/to/file.txt"
```

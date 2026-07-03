---
sidebar_position: 1
---

# storage blob copy
Copies a blob to a destination container, optionally across accounts.

## Options
* `--source-account-name` - (Required) (Required) Source storage account name.
* `--source-container` - (Required) (Required) Source container name.
* `--source-blob` - (Required) (Required) Source blob name.
* `--source-resource-group` - (Required) (Required) Source resource group name.
* `--source-subscription-id` - (Required) (Required) Source subscription ID.
* `--dest-account-name` - (Required) (Required) Destination storage account name.
* `--dest-container` - (Required) (Required) Destination container name.
* `--dest-blob` - (Required) (Required) Destination blob name.
* `--dest-resource-group` - (Required) (Required) Destination resource group name.
* `--dest-subscription-id` - (Required) (Required) Destination subscription ID.

## Examples

### Copy blob within same account
```bash
$ topaz storage blob copy \
    --source-subscription-id "00000000-0000-0000-0000-000000000000" \
    --source-resource-group "rg-local" \
    --source-account-name "salocal" \
    --source-container "src" \
    --source-blob "file.txt" \
    --dest-subscription-id "00000000-0000-0000-0000-000000000000" \
    --dest-resource-group "rg-local" \
    --dest-account-name "salocal" \
    --dest-container "dst" \
    --dest-blob "file-copy.txt"
```

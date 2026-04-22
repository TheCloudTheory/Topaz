---
sidebar_position: 7
---

# storage table show
Shows whether a table exists in a storage account.

## Options
* `-n, --name` - (Required) (Required) Table name.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Show a table
```bash
$ topaz storage table show \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --name "mytable"
```

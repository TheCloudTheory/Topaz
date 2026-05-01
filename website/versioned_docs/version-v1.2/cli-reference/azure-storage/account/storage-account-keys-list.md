---
sidebar_position: 6
---

# storage account keys list
Lists the access keys for a storage account.

## Options
* `-n, --account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### List storage account keys
```bash
$ topaz storage account keys list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal"
```

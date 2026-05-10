---
sidebar_position: 3
---

# storage container list
Lists blob containers in a storage account.

## Options
* `-n, --account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### List containers
```bash
$ topaz storage container list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal"
```

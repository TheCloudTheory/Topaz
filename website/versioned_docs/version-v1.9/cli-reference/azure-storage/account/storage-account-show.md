---
sidebar_position: 9
---

# storage account show
Shows details of an Azure Storage account.

## Options
* `-n, --name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Show a storage account
```bash
$ topaz storage account show \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "salocal"
```

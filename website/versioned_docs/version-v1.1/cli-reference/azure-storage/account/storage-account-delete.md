---
sidebar_position: 3
---

# storage account delete
Deletes an Azure Storage account.

## Options
* `-n, --name` - (Required) (Required) Storage account name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete a storage account
```bash
$ topaz storage account delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "salocal"
```

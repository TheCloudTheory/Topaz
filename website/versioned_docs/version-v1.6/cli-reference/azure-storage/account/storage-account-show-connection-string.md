---
sidebar_position: 10
---

# storage account show-connection-string
Shows the connection string for a storage account.

## Options
* `-n, --name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Show connection string
```bash
$ topaz storage account show-connection-string \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "salocal"
```

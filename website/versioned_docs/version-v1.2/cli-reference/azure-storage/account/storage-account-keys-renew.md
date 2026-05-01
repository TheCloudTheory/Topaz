---
sidebar_position: 7
---

# storage account keys renew
Regenerates an access key for a storage account.

## Options
* `-n, --account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-k, --key-name` - (Required) (Required) The key to regenerate (key1 or key2).

## Examples

### Regenerate key1
```bash
$ topaz storage account keys renew \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --key-name "key1"
```

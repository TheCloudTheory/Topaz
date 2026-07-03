---
sidebar_position: 1
---

# storage account check-name
Checks whether a storage account name is available.

## Options
* `-n, --name` - (Required) (Required) Storage account name to check.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Check name availability
```bash
$ topaz storage account check-name \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --name "salocal"
```

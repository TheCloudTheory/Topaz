---
sidebar_position: 8
---

# storage account list
Lists Azure Storage accounts.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - Resource group name (filters to accounts in this group when specified).

## Examples

### List all accounts in a subscription
```bash
$ topaz storage account list \
    --subscription-id "00000000-0000-0000-0000-000000000000"
```

### List accounts in a resource group
```bash
$ topaz storage account list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local"
```

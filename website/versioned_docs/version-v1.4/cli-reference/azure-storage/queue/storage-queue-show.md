---
sidebar_position: 4
---

# storage queue show
Shows properties of a queue in a storage account.

## Options
* `-n, --name` - (Required) (Required) Queue name.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Show a queue
```bash
$ topaz storage queue show \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --name "myqueue"
```

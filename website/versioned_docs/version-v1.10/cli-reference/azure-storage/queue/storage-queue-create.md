---
sidebar_position: 1
---

# storage queue create
Creates a new queue in a storage account.

## Options
* `-n, --name` - (Required) (Required) Queue name.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Create a queue
```bash
$ topaz storage queue create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --name "myqueue"
```

---
sidebar_position: 11
---

# storage account update
Updates an Azure Storage account.

## Options
* `-n, --name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--tags` - Resource tags as key=value pairs.

## Examples

### Update tags on a storage account
```bash
$ topaz storage account update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "salocal" \
    --tags "env=prod" "owner=team"
```

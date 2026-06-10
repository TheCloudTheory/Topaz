---
sidebar_position: 2
---

# storage account create
Creates a new Azure Storage account.

## Options
* `-n, --name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Location (e.g. westeurope).
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Create a storage account
```bash
$ topaz storage account create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "salocal" \
    --location "westeurope"
```

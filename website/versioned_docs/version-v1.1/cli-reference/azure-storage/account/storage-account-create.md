---
sidebar_position: 1
---

# storage account create

Creates a new storage account.

## Options
* `-s|--subscription-id` - (Required) subscription ID
* `-n|--name` - (Required) storage account name
* `-l|--location` - (Required) storage account name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Create new storage account
```bash
$ topaz storage account create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "salocal" \
    --location "westeurope" \
    --resource-group "rg-local"
```
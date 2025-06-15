---
sidebar_position: 1
---

# keyvault create

Creates a new Azure Key Vault.

## Options
* `-s|--subscription-id` - (Required) subscription ID
* `-n|--name` - (Required) vault name
* `-l|--location` - (Required) storage account name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Create new vault
```bash
topaz keyvault create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "kv-local" \
    --location "westeurope" \
    --resource-group "rg-local"
```
---
sidebar_position: 2
---

# keyvault create
Creates a new Azure Key Vault.

## Options
* `-n, --name` - (Required) vault name
* `-g, --resource-group` - (Required) resource group name
* `-l, --location` - (Required) Key Vault location
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Creates a new Key Vault
```bash
$ topaz keyvault create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "kvlocal" \
    --location "westeurope" \
    --resource-group "rg-local"
```

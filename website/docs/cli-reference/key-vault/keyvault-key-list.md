---
sidebar_position: 23
---

# keyvault key list
Lists all keys in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### List all keys in a vault
```bash
$ topaz keyvault key list --vault-name "kvlocal" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

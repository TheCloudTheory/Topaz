---
sidebar_position: 28
---

# keyvault key list-versions
Lists all versions of a key in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### List key versions
```bash
$ topaz keyvault key list-versions --vault-name "kvlocal" --name "my-key" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

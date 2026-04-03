---
sidebar_position: 7
---

# keyvault secret list-deleted
Lists all deleted secrets in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### List deleted secrets
```bash
$ topaz keyvault secret list-deleted --vault-name "kvlocal" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

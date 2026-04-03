---
sidebar_position: 9
---

# keyvault secret list-versions
Lists all versions of a secret in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Secret name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### List secret versions
```bash
$ topaz keyvault secret list-versions --vault-name "kvlocal" --name "my-secret" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

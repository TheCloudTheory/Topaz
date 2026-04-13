---
sidebar_position: 5
---

# keyvault secret get-deleted
Gets a deleted secret from an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Secret name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Get a deleted secret
```bash
$ topaz keyvault secret get-deleted --vault-name "kvlocal" --name "my-secret" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

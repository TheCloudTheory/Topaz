---
sidebar_position: 10
---

# keyvault secret recover
Recovers a deleted secret in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Secret name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Recover a deleted secret
```bash
$ topaz keyvault secret recover --vault-name "kvlocal" --name "my-secret" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

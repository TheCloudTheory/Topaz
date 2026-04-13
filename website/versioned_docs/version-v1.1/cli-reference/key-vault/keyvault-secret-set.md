---
sidebar_position: 13
---

# keyvault secret set
Sets a secret in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Secret name.
* `--value` - (Required) (Required) Secret value.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Set a secret
```bash
$ topaz keyvault secret set --vault-name "kvlocal" --name "my-secret" --value "my-value" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

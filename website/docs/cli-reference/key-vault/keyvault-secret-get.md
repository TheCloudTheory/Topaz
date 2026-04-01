---
sidebar_position: 4
---

# keyvault secret get
Gets a secret from an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Secret name.
* `--version` - Secret version. Defaults to the latest version.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Get a secret
```bash
$ topaz keyvault secret get --vault-name "kvlocal" --name "my-secret" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

---
sidebar_position: 15
---

# keyvault secret update
Updates the attributes of a secret in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Secret name.
* `--version` - (Required) (Required) Secret version.
* `--enabled` - Enable or disable the secret.
* `--content-type` - Content type of the secret.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Disable a secret
```bash
$ topaz keyvault secret update --vault-name "kvlocal" --name "my-secret" --version "<version-guid>" --enabled false --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

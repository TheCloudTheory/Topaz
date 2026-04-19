---
sidebar_position: 21
---

# keyvault key update
Updates the attributes of a key version in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--version` - (Required) (Required) Key version.
* `--enabled` - Enable or disable the key version.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Disable a key version
```bash
$ topaz keyvault key update --vault-name "kvlocal" --name "my-key" --version "<version-guid>" --enabled false --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

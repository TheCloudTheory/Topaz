---
sidebar_position: 33
---

# keyvault key rotate
Rotates a key in an Azure Key Vault by creating a new version with fresh cryptographic material of the same type.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Rotate a key
```bash
$ topaz keyvault key rotate --vault-name "kvlocal" --name "my-key" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

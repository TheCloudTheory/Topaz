---
sidebar_position: 53
---

# keyvault certificate recover
Recovers a soft-deleted certificate in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Certificate name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Recover a deleted certificate
```bash
$ topaz keyvault certificate recover --vault-name "kvlocal" --name "my-cert" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

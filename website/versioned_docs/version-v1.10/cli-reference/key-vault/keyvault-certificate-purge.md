---
sidebar_position: 52
---

# keyvault certificate purge
Permanently deletes a soft-deleted certificate from an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Certificate name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Purge a deleted certificate
```bash
$ topaz keyvault certificate purge --vault-name "kvlocal" --name "my-cert" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

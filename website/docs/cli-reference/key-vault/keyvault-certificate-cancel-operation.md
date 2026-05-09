---
sidebar_position: 41
---

# keyvault certificate cancel-operation
Cancels the pending creation operation for a certificate in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Certificate name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Cancel a certificate pending operation
```bash
$ topaz keyvault certificate cancel-operation --vault-name "kvlocal" --name "my-cert" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

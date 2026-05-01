---
sidebar_position: 37
---

# keyvault key rotation-policy update
Updates the rotation policy for a key in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--expires-in` - The expiry time as an ISO 8601 duration (e.g. P2Y). If omitted, the expiry is cleared.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Set a 2-year expiry on a key rotation policy
```bash
$ topaz keyvault key rotation-policy update --vault-name "kvlocal" --name "my-key" --expires-in "P2Y" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

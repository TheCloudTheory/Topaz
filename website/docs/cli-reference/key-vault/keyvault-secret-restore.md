---
sidebar_position: 7
---

# keyvault secret restore
Restores a secret into an Azure Key Vault from a backup blob.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `--backup-value` - (Required) (Required) Base64url-encoded backup blob produced by the backup command.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Restore a secret
```bash
$ topaz keyvault secret restore --vault-name "kvlocal" --backup-value "<encoded blob>" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

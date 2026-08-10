---
sidebar_position: 45
---

# keyvault certificate get
Gets a certificate from an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Certificate name.
* `--version` - (Optional) Certificate version. Omit to get the latest.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Get a certificate
```bash
$ topaz keyvault certificate get --vault-name "kvlocal" --name "my-cert" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

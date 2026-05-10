---
sidebar_position: 55
---

# keyvault certificate update
Updates the attributes of a certificate in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Certificate name.
* `--version` - (Required) (Required) Certificate version.
* `--enabled` - (Optional) Enable or disable the certificate.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Disable a certificate
```bash
$ topaz keyvault certificate update --vault-name "kvlocal" --name "my-cert" --version "<version-guid>" --enabled false --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

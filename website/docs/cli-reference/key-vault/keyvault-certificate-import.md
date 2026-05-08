---
sidebar_position: 45
---

# keyvault certificate import
Imports a PFX/PKCS#12 certificate into an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Certificate name.
* `--value` - (Required) (Required) Base64-encoded PFX/PKCS#12 certificate bytes.
* `--password` - (Optional) Password for the PFX file.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Import a certificate
```bash
$ topaz keyvault certificate import --vault-name "kvlocal" --name "my-cert" --value "<base64-pfx>" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

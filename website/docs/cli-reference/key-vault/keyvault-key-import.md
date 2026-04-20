---
sidebar_position: 21
---

# keyvault key import
Imports an externally created key into an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--pem-file` - (Required) (Required) Path to a PEM-encoded RSA or EC key file.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Import an RSA key from a PEM file
```bash
$ topaz keyvault key import --vault-name "kvlocal" --name "my-imported-key" --pem-file "/path/to/key.pem" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

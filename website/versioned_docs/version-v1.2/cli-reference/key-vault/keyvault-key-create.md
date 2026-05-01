---
sidebar_position: 17
---

# keyvault key create
Creates a key in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--kty` - Key type (RSA, EC). Defaults to RSA.
* `--key-size` - RSA key size in bits (2048, 3072, 4096). Defaults to 2048.
* `--crv` - EC curve name (P-256, P-384, P-521). Defaults to P-256 for EC keys.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Create an RSA key
```bash
$ topaz keyvault key create --vault-name "kvlocal" --name "my-rsa-key" --kty RSA --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

### Create an EC key
```bash
$ topaz keyvault key create --vault-name "kvlocal" --name "my-ec-key" --kty EC --crv P-256 --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

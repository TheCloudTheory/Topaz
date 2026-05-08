---
sidebar_position: 40
---

# keyvault certificate create
Creates a self-signed certificate in an Azure Key Vault.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Certificate name.
* `--subject` - (Optional) X.509 subject (e.g. CN=my-cert). Defaults to CN=&lt;name&gt;.
* `--validity-months` - (Optional) Validity in months. Defaults to 12.
* `--key-size` - (Optional) RSA key size in bits. Defaults to 2048.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Create a certificate
```bash
$ topaz keyvault certificate create --vault-name "kvlocal" --name "my-cert" --subject "CN=my-cert" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

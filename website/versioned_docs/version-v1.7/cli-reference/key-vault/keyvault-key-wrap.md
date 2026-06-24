---
sidebar_position: 39
---

# keyvault key wrap
Wraps (encrypts) a key using a Key Vault key (RSA keys only: RSA1_5, RSA-OAEP, RSA-OAEP-256).

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--version` - (Required) (Required) Key version.
* `-a, --algorithm` - (Required) (Required) Wrap algorithm. Supported: RSA1_5, RSA-OAEP, RSA-OAEP-256.
* `--value` - (Required) (Required) Base64url-encoded key material to wrap.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Wrap with RSA-OAEP-256
```bash
$ topaz keyvault key wrap --vault-name "kvlocal" --name "my-key" --version "abc123" --algorithm "RSA-OAEP-256" --value "aGVsbG8=" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

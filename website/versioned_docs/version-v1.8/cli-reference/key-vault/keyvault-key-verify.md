---
sidebar_position: 38
---

# keyvault key verify
Verifies a signature against a pre-hashed digest using a Key Vault key (RSA: RS256, RS384, RS512, PS256, PS384, PS512; EC: ES256, ES384, ES512).

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--version` - (Required) (Required) Key version.
* `-a, --algorithm` - (Required) (Required) Verification algorithm. Supported: RS256, RS384, RS512, PS256, PS384, PS512, ES256, ES384, ES512.
* `--value` - (Required) (Required) Base64url-encoded pre-hashed digest.
* `--signature` - (Required) (Required) Base64url-encoded signature to verify.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Verify a signature with RS256
```bash
$ topaz keyvault key verify --vault-name "kvlocal" --name "my-key" --version "abc123" --algorithm "RS256" --value "<base64url-digest>" --signature "<base64url-signature>" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

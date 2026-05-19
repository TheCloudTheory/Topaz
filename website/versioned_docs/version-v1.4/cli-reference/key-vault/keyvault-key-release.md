---
sidebar_position: 31
---

# keyvault key release
Releases a key for Secure Key Release (SKR). Any non-empty attestation token is accepted by the emulator.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--version` - (Required) (Required) Key version.
* `--target` - (Required) (Required) Attestation assertion (target). Any non-empty value is accepted by the emulator.
* `--enc` - (Optional) Encryption algorithm hint. Accepted but not enforced by the emulator.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Release a key
```bash
$ topaz keyvault key release --vault-name "kvlocal" --name "my-key" --version "abc123" --target "mytoken" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

---
sidebar_position: 22
---

# keyvault key get-attestation
Gets a key and its attestation information from an Azure Key Vault. For software-backed keys the attestation field is null, matching real Azure behaviour.

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--version` - Key version (omit to retrieve the latest version).
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Get attestation for the latest version of a key
```bash
$ topaz keyvault key get-attestation --vault-name "kvlocal" --name "my-key" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

### Get attestation for a specific version of a key
```bash
$ topaz keyvault key get-attestation --vault-name "kvlocal" --name "my-key" --version "abc123" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

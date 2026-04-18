---
sidebar_position: 16
---

# keyvault key get
Gets a key from an Azure Key Vault (latest version or a specific version).

## Options
* `--vault-name` - (Required) (Required) Key Vault name.
* `-n, --name` - (Required) (Required) Key name.
* `--version` - Key version (omit to retrieve the latest version).
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Get the latest version of a key
```bash
$ topaz keyvault key get --vault-name "kvlocal" --name "my-key" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

### Get a specific version of a key
```bash
$ topaz keyvault key get --vault-name "kvlocal" --name "my-key" --version "abc123" --resource-group "rg-local" --subscription-id "36a28ebb-9370-46d8-981c-84efe02048ae"
```

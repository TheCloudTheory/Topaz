---
sidebar_position: 1
---

# keyvault check-name
Checks if the provided Key Vault name is available.

## Options
* `-n, --name` - (Required) Key Vault name.
* `--resource-type` - Type of Key Vault to create.
* `-s, --subscription-id` - (Required) Key Vault subscription ID.

## Examples

### Check Key Vault name
```bash
$ topaz keyvault check-name \
    --name "sb-namespace" \
    --resource-group "rg" \
    --subscription-id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A"
```

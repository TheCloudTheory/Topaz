---
sidebar_position: 3
---

# keyvault delete
Deletes a Key Vault.

## Options
* `-n, --name` - (Required) (Required) Key Vault name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Delete a Key Vault
```bash
$ topaz keyvault delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "kvlocal"
```

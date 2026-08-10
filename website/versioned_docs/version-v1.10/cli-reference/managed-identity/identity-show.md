---
sidebar_position: 4
---

# identity show
Shows details of a user-assigned managed identity.

## Options
* `-n, --name` - (Required) managed identity name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Shows a managed identity
```bash
$ topaz identity show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "myIdentity" \
    --resource-group "rg-local"
```

---
sidebar_position: 1
---

# identity create
Creates a new user-assigned managed identity.

## Options
* `-n, --name` - (Required) managed identity name
* `-g, --resource-group` - (Required) resource group name
* `-l, --location` - (Required) location
* `-s, --subscription-id` - (Required) subscription ID
* `--tags` - (Optional) resource tags
* `--isolation-scope` - (Optional) isolation scope (None or Regional)

## Examples

### Creates a new managed identity
```bash
$ topaz identity create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "myIdentity" \
    --location "westeurope" \
    --resource-group "rg-local"
```

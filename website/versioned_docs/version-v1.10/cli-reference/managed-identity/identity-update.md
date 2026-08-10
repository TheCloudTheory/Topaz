---
sidebar_position: 5
---

# identity update
Updates a user-assigned managed identity.

## Options
* `-n, --name` - (Required) managed identity name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID
* `--tags` - (Optional) resource tags
* `--isolation-scope` - (Optional) isolation scope (None or Regional)

## Examples

### Updates a managed identity with tags
```bash
$ topaz identity update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "myIdentity" \
    --resource-group "rg-local" \
    --tags environment=production team=devops
```

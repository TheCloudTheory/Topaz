---
sidebar_position: 4
---

# acr repository delete
Deletes a repository from an Azure Container Registry.

## Options
* `-n, --name` - (Required) Repository name.
* `-r, --registry` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.

## Examples

### Delete a repository
```bash
$ topaz acr repository delete \
+    --subscription-id "00000000-0000-0000-0000-000000000000" \
+    --resource-group "my-rg" \
+    --registry "myregistry" \
+    --name "sample-repository"
```

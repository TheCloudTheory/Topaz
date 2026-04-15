---
sidebar_position: 9
---

# acr repository list
Lists repositories in an Azure Container Registry.

## Options
* `-r, --registry` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.

## Examples

### List repositories in a registry
```bash
$ topaz acr repository list \
+    --subscription-id "00000000-0000-0000-0000-000000000000" \
+    --resource-group "my-rg" \
+    --registry "myregistry"
```

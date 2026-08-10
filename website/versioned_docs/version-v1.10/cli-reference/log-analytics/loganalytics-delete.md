---
sidebar_position: 2
---

# loganalytics delete
Deletes an Azure Log Analytics workspace.

## Options
* `-n, --name` - (Required) (Required) Workspace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Deletes a Log Analytics workspace
```bash
$ topaz loganalytics delete --subscription-id 36a28ebb-9370-46d8-981c-84efe0204800 \
    --name "my-workspace" \
    --resource-group "rg-local"
```

---
sidebar_position: 4
---

# loganalytics list
Lists Azure Log Analytics workspaces in a subscription or resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - (Optional) Filter by resource group name.

## Examples

### Lists Log Analytics workspaces in a resource group
```bash
$ topaz loganalytics list --subscription-id 36a28ebb-9370-46d8-981c-84efe0204800 \
    --resource-group "rg-local"
```

---
sidebar_position: 4
---

# insights component list
Lists Application Insights components in a subscription or resource group.

## Options
* `-s, --subscription-id` - (Required) subscription ID
* `-g, --resource-group` - (Optional) filter by resource group name

## Examples

### Lists Application Insights components in a resource group
```bash
$ topaz insights component list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

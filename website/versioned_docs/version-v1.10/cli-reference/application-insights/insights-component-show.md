---
sidebar_position: 3
---

# insights component show
Gets an Application Insights component.

## Options
* `-n, --name` - (Required) component name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets an Application Insights component
```bash
$ topaz insights component show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-appinsights" \
    --resource-group "rg-local"
```

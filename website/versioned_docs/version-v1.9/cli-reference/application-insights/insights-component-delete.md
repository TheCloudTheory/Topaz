---
sidebar_position: 2
---

# insights component delete
Deletes an Application Insights component.

## Options
* `-n, --name` - (Required) component name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes an Application Insights component
```bash
$ topaz insights component delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-appinsights" \
    --resource-group "rg-local"
```

---
sidebar_position: 1
---

# insights component create
Creates or updates an Application Insights component.

## Options
* `-n, --name` - (Required) component name
* `-g, --resource-group` - (Required) resource group name
* `-l, --location` - (Required) location
* `-s, --subscription-id` - (Required) subscription ID
* `--application-type` - Application type (default: web)
* `--kind` - Kind (default: web)

## Examples

### Creates a new Application Insights component
```bash
$ topaz insights component create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-appinsights" \
    --resource-group "rg-local" \
    --location "westeurope"
```

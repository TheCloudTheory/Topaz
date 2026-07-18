---
sidebar_position: 5
---

# insights component update
Updates tags or properties of an Application Insights component.

## Options
* `-n, --name` - (Required) component name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID
* `--retention-in-days` - Data retention in days
* `--public-network-access-for-ingestion` - Public network access for ingestion (Enabled/Disabled)

## Examples

### Updates the retention period of an Application Insights component
```bash
$ topaz insights component update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-appinsights" \
    --resource-group "rg-local" \
    --retention-in-days 180
```

---
sidebar_position: 14
---

# apim product list-apis
Lists APIs assigned to a product in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--product-id` - (Required) product identifier
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Lists APIs assigned to a product
```bash
$ topaz apim product list-apis --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --product-id "my-product" \
    --resource-group "rg-local"
```

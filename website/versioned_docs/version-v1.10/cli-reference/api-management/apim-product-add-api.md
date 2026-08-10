---
sidebar_position: 8
---

# apim product add-api
Assigns an API to a product in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--product-id` - (Required) product identifier
* `--api-id` - (Required) API identifier
* `--if-match` - (Optional) ETag for conditional update
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Assigns an API to a product
```bash
$ topaz apim product add-api --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --product-id "my-product" \
    --api-id "my-api" \
    --resource-group "rg-local"
```

---
sidebar_position: 9
---

# apim product create
Creates or updates a product in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--product-id` - (Required) product identifier
* `--display-name` - (Required) display name of the product
* `--description` - (Optional) description of the product
* `--terms` - (Optional) terms of use
* `--subscription-required` - (Optional) whether a subscription is required to access the product
* `--approval-needed` - (Optional) whether approval is needed to subscribe
* `--state` - (Optional) product state (notPublished or published)
* `--if-match` - (Optional) ETag for conditional update
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Creates a new product in an API Management service
```bash
$ topaz apim product create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --product-id "my-product" \
    --display-name "My Product" \
    --resource-group "rg-local"
```

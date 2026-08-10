---
sidebar_position: 16
---

# apim product update
Updates a product in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--product-id` - (Required) product identifier
* `--display-name` - (Optional) new display name
* `--description` - (Optional) description
* `--terms` - (Optional) terms of use
* `--subscription-required` - (Optional) whether a subscription is required to access the product
* `--approval-needed` - (Optional) whether approval is needed to subscribe
* `--state` - (Optional) product state (notPublished or published)
* `--if-match` - (Optional) ETag for conditional update
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Updates a product display name
```bash
$ topaz apim product update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --product-id "my-product" \
    --display-name "Updated Product" \
    --resource-group "rg-local"
```

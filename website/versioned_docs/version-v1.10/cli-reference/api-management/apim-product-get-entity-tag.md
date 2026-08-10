---
sidebar_position: 13
---

# apim product get-entity-tag
Gets the entity tag (ETag) for a product in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--product-id` - (Required) product identifier
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets the ETag for a product
```bash
$ topaz apim product get-entity-tag --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --product-id "my-product" \
    --resource-group "rg-local"
```

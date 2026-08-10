---
sidebar_position: 11
---

# apim product delete
Deletes a product in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--product-id` - (Required) product identifier
* `--if-match` - (Optional) ETag for conditional delete
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes a product in an API Management service
```bash
$ topaz apim product delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --product-id "my-product" \
    --resource-group "rg-local"
```

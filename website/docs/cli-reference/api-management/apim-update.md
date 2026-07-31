---
sidebar_position: 6
---

# apim update
Updates an Azure API Management service.

## Options
* `-n, --name` - (Required) API Management service name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID
* `--sku-name` - (Optional) SKU name (e.g. Developer, Basic, Standard, Premium)
* `--sku-capacity` - (Optional) SKU capacity
* `--publisher-email` - (Optional) publisher email address
* `--publisher-name` - (Optional) publisher name

## Examples

### Updates an API Management service SKU
```bash
$ topaz apim update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-apim" \
    --resource-group "rg-local" \
    --sku-name "Standard" \
    --sku-capacity 2
```

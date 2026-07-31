---
sidebar_position: 2
---

# apim create
Creates or updates an Azure API Management service.

## Options
* `-n, --name` - (Required) API Management service name
* `-g, --resource-group` - (Required) resource group name
* `-l, --location` - (Required) location
* `-s, --subscription-id` - (Required) subscription ID
* `--publisher-email` - (Required) publisher email address
* `--publisher-name` - (Required) publisher name
* `--sku-name` - SKU name (e.g. Developer, Basic, Standard, Premium)
* `--sku-capacity` - SKU capacity

## Examples

### Creates a new API Management service
```bash
$ topaz apim create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-apim" \
    --location "westeurope" \
    --resource-group "rg-local" \
    --publisher-email "admin@example.com" \
    --publisher-name "My Company" \
    --sku-name "Developer" \
    --sku-capacity 1
```

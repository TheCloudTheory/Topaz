---
sidebar_position: 3
---

# apim delete
Deletes an Azure API Management service.

## Options
* `-n, --name` - (Required) API Management service name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes an API Management service
```bash
$ topaz apim delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-apim" \
    --resource-group "rg-local"
```

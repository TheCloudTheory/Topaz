---
sidebar_position: 19
---

# apim api show
Gets an API in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--api-id` - (Required) API identifier
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets an API in an API Management service
```bash
$ topaz apim api show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --api-id "my-api" \
    --resource-group "rg-local"
```

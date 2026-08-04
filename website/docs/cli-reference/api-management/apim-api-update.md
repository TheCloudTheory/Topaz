---
sidebar_position: 30
---

# apim api update
Updates an API in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--api-id` - (Required) API identifier
* `--display-name` - (Optional) new display name
* `--path` - (Optional) relative URL path
* `--service-url` - (Optional) backend service URL
* `--description` - (Optional) description
* `--if-match` - (Optional) ETag for conditional update
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Updates an API display name
```bash
$ topaz apim api update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --api-id "my-api" \
    --display-name "Updated API" \
    --resource-group "rg-local"
```

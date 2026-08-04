---
sidebar_position: 24
---

# apim api create
Creates or updates an API in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--api-id` - (Required) API identifier
* `--display-name` - (Optional) display name of the API
* `--path` - (Optional) relative URL path for the API
* `--protocols` - (Optional) comma-separated protocols (e.g. http,https)
* `--service-url` - (Optional) backend service URL
* `--description` - (Optional) description of the API
* `--api-type` - (Optional) API type (http, soap, websocket, graphql)
* `--if-match` - (Optional) ETag for conditional update
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Creates a new API in an API Management service
```bash
$ topaz apim api create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --api-id "my-api" \
    --display-name "My API" \
    --path "/myapi" \
    --resource-group "rg-local"
```

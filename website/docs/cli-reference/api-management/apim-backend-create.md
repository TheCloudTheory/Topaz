---
sidebar_position: 22
---

# apim backend create
Creates or updates a backend in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--backend-id` - (Required) backend identifier
* `--url` - (Optional) runtime URL of the backend
* `--protocol` - (Optional) backend communication protocol (http or soap)
* `--description` - (Optional) backend description
* `--title` - (Optional) backend title
* `--resource-id` - (Optional) management URI of the backend in external system
* `--type` - (Optional) type of the backend (Single or Pool)
* `--if-match` - (Optional) ETag for conditional update
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Creates a new backend in an API Management service
```bash
$ topaz apim backend create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --backend-id "my-backend" \
    --url "https://backend.example.com" \
    --protocol "http" \
    --resource-group "rg-local"
```

---
sidebar_position: 28
---

# apim backend update
Updates a backend in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--backend-id` - (Required) backend identifier
* `--url` - (Optional) runtime URL of the backend
* `--protocol` - (Optional) backend communication protocol (http or soap)
* `--description` - (Optional) backend description
* `--title` - (Optional) backend title
* `--resource-id` - (Optional) management URI of the backend in external system
* `--if-match` - (Optional) ETag for conditional update
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Updates a backend URL
```bash
$ topaz apim backend update --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --backend-id "my-backend" \
    --url "https://new-backend.example.com" \
    --resource-group "rg-local"
```

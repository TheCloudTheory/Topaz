---
sidebar_position: 18
---

# apim api delete
Deletes an API in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--api-id` - (Required) API identifier
* `--delete-revisions` - (Optional) delete all revisions of the API
* `--if-match` - (Optional) ETag for conditional delete
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes an API in an API Management service
```bash
$ topaz apim api delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --api-id "my-api" \
    --resource-group "rg-local"
```

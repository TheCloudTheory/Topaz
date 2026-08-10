---
sidebar_position: 32
---

# apim api get-entity-tag
Gets the entity tag (ETag) for an API in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--api-id` - (Required) API identifier
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets the ETag for an API
```bash
$ topaz apim api get-entity-tag --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --api-id "my-api" \
    --resource-group "rg-local"
```

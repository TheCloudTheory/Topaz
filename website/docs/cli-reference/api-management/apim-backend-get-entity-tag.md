---
sidebar_position: 20
---

# apim backend get-entity-tag
Gets the entity tag (ETag) for a backend in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--backend-id` - (Required) backend identifier
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets the ETag for a backend
```bash
$ topaz apim backend get-entity-tag --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --backend-id "my-backend" \
    --resource-group "rg-local"
```

---
sidebar_position: 23
---

# apim backend delete
Deletes a backend in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--backend-id` - (Required) backend identifier
* `--if-match` - (Optional) ETag for conditional delete
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes a backend in an API Management service
```bash
$ topaz apim backend delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --backend-id "my-backend" \
    --resource-group "rg-local"
```

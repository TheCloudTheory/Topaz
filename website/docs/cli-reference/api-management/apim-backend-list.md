---
sidebar_position: 26
---

# apim backend list
Lists backends in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Lists backends in an API Management service
```bash
$ topaz apim backend list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --resource-group "rg-local"
```

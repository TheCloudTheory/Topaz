---
sidebar_position: 19
---

# apim policy show
Gets a policy in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--policy-id` - (Required) policy identifier
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets a policy in an API Management service
```bash
$ topaz apim policy show --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --policy-id "policy" \
    --resource-group "rg-local"
```

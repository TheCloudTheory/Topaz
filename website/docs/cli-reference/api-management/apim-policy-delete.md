---
sidebar_position: 18
---

# apim policy delete
Deletes a policy in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--policy-id` - (Required) policy identifier
* `--if-match` - (Optional) ETag for optimistic concurrency
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Deletes a policy in an API Management service
```bash
$ topaz apim policy delete --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --policy-id "policy" \
    --resource-group "rg-local"
```

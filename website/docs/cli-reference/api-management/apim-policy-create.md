---
sidebar_position: 17
---

# apim policy create
Creates or updates a policy in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--policy-id` - (Required) policy identifier
* `--value` - (Required) policy content
* `--format` - (Optional) policy content format (default: xml)
* `--if-match` - (Optional) ETag for optimistic concurrency
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Creates a policy in an API Management service
```bash
$ topaz apim policy create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --policy-id "policy" \
    --value "<policies><inbound /></policies>" \
    --resource-group "rg-local"
```

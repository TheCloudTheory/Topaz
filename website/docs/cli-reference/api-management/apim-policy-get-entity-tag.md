---
sidebar_position: 20
---

# apim policy get-entity-tag
Gets the entity tag (ETag) for a policy in an Azure API Management service.

## Options
* `--service-name` - (Required) API Management service name
* `--policy-id` - (Required) policy identifier
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Gets the ETag for a policy
```bash
$ topaz apim policy get-entity-tag --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --policy-id "policy" \
    --resource-group "rg-local"
```

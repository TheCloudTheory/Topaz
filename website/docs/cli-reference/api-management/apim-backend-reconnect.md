---
sidebar_position: 27
---

# apim backend reconnect
Notifies API Management to create a new connection to the backend after the specified timeout.

## Options
* `--service-name` - (Required) API Management service name
* `--backend-id` - (Required) backend identifier
* `--after` - (Optional) duration after which reconnect is initiated (ISO 8601 duration, e.g. PT3S)
* `-g, --resource-group` - (Required) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Triggers reconnect for a backend
```bash
$ topaz apim backend reconnect --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --service-name "my-apim" \
    --backend-id "my-backend" \
    --resource-group "rg-local"
```

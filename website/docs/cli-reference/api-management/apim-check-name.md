---
sidebar_position: 1
---

# apim check-name
Checks whether an API Management service name is available.

## Options
* `-n, --name` - (Required) API Management service name to check
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Check API Management service name availability
```bash
$ topaz apim check-name --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --name "my-apim"
```

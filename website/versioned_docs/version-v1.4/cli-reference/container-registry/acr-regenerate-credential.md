---
sidebar_position: 10
---

# acr regenerate-credential
Regenerates an admin password for an Azure Container Registry.

## Options
* `-n, --name` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.
* `--password-name` - (Required) Password to regenerate: 'password' or 'password2'.

## Examples

### Regenerate password
```bash
$ topaz acr regenerate-credential \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry" \
    --password-name password
```

### Regenerate password2
```bash
$ topaz acr regenerate-credential \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry" \
    --password-name password2
```

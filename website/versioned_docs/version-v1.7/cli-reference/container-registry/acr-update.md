---
sidebar_position: 12
---

# acr update
Updates an Azure Container Registry.

## Options
* `-n, --name` - (Required) Registry name.
* `-g, --resource-group` - (Required) Resource group name.
* `-s, --subscription-id` - (Required) Subscription ID.
* `--sku` - SKU name: Basic, Standard, or Premium.
* `--admin-enabled` - Enable or disable the admin user (true/false).
* `--tags` - Resource tags (key=value pairs). Replaces existing tags.

## Examples

### Enable admin user
```bash
$ topaz acr update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry" \
    --admin-enabled true
```

### Change SKU and set tags
```bash
$ topaz acr update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry" \
    --sku Premium \
    --tags env=prod team=ops
```

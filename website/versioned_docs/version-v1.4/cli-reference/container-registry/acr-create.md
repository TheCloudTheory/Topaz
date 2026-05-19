---
sidebar_position: 2
---

# acr create
Creates a new Azure Container Registry.

## Options
* `-n, --name` - (Required) Registry name (5-50 alphanumeric characters).
* `-g, --resource-group` - (Required) Resource group name.
* `-l, --location` - (Required) Registry location.
* `-s, --subscription-id` - (Required) Subscription ID.
* `--sku` - SKU name: Basic, Standard, or Premium. Defaults to Basic.
* `--admin-enabled` - Enable the admin user.
* `--tags` - Resource tags (key=value pairs).

## Examples

### Create a Basic registry
```bash
$ topaz acr create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry" \
    --location "westeurope"
```

### Create a Standard registry with admin user
```bash
$ topaz acr create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-rg" \
    --name "myregistry" \
    --location "westeurope" \
    --sku Standard \
    --admin-enabled
```

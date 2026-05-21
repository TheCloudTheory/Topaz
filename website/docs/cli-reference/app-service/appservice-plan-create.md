---
sidebar_position: 1
---

# appservice plan create
Creates or updates an App Service Plan.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Plan name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `--sku-name` - (Optional) SKU name (e.g. B1, S1, P1v2). Defaults to B1.

## Examples

### Create a plan
```bash
$ topaz appservice plan create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-plan" \
    --location "westeurope" \
    --sku-name "B1"
```

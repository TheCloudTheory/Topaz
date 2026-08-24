---
sidebar_position: 1
---

# eventgrid namespace create
Creates or updates an Event Grid Namespace.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `--sku-name` - (Optional) SKU name (e.g. Standard).
* `--sku-capacity` - (Optional) SKU capacity.

## Examples

### Create an Event Grid Namespace
```bash
$ topaz eventgrid namespace create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-namespace" \
    --location "westeurope"
```

---
sidebar_position: 17
---

# eventgrid namespace update
Updates an Event Grid Namespace.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Event Grid Namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `--sku-name` - (Optional) SKU name (e.g. Standard).
* `--sku-capacity` - (Optional) SKU capacity.

## Examples

### Update an Event Grid Namespace
```bash
$ topaz eventgrid namespace update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-namespace" \
    --sku-name "Standard"
```

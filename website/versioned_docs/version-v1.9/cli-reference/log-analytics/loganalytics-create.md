---
sidebar_position: 1
---

# loganalytics create
Creates or updates an Azure Log Analytics workspace.

## Options
* `-n, --name` - (Required) (Required) Workspace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Location.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--sku` - (Optional) SKU name (e.g. PerGB2018, Free, CapacityReservation).
* `--retention-in-days` - (Optional) Retention in days.

## Examples

### Creates a new Log Analytics workspace
```bash
$ topaz loganalytics create --subscription-id 36a28ebb-9370-46d8-981c-84efe0204800 \
    --name "my-workspace" \
    --location "westeurope" \
    --resource-group "rg-local"
```

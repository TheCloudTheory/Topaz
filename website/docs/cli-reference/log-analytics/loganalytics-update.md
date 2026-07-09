---
sidebar_position: 5
---

# loganalytics update
Updates an Azure Log Analytics workspace (tags, SKU, retentionInDays).

## Options
* `-n, --name` - (Required) (Required) Workspace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--tags` - (Optional) Tags in key=value format.
* `--sku` - (Optional) SKU name (e.g. PerGB2018, Free, CapacityReservation).
* `--retention-in-days` - (Optional) Retention in days.

## Examples

### Updates a Log Analytics workspace's retention
```bash
$ topaz loganalytics update --subscription-id 36a28ebb-9370-46d8-981c-84efe0204800 \
    --name "my-workspace" \
    --resource-group "rg-local" \
    --retention-in-days 60
```

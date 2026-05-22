---
sidebar_position: 9
---

# appservice site list
Lists Web Apps and Function Apps in a resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List sites in a resource group
```bash
$ topaz appservice site list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local"
```

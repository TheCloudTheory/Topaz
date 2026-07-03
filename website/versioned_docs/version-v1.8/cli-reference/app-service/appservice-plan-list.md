---
sidebar_position: 9
---

# appservice plan list
Lists App Service Plans in a resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List plans in a resource group
```bash
$ topaz appservice plan list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local"
```

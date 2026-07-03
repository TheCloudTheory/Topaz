---
sidebar_position: 4
---

# appservice plan delete
Deletes an App Service Plan.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Plan name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete a plan
```bash
$ topaz appservice plan delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-plan"
```

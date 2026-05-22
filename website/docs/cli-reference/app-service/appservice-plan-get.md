---
sidebar_position: 6
---

# appservice plan get
Gets an App Service Plan.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Plan name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Get a plan
```bash
$ topaz appservice plan get \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-plan"
```

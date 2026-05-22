---
sidebar_position: 10
---

# appservice plan restart-sites
Restarts all Web Apps in an App Service Plan.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Plan name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Restart sites in a plan
```bash
$ topaz appservice plan restart-sites \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-plan"
```

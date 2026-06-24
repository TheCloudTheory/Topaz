---
sidebar_position: 11
---

# appservice site config list-appsettings
Lists the current application settings of a Web App (Microsoft.Web/sites/config/appsettings/list).

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Site name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List app settings
```bash
$ topaz appservice site config list-appsettings \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-site"
```

---
sidebar_position: 13
---

# appservice site config set-appsettings
Replaces all application settings of a Web App (Microsoft.Web/sites/config/appsettings).

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Site name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `--settings` - Application settings as KEY=VALUE pairs.

## Examples

### Set app settings
```bash
$ topaz appservice site config set-appsettings \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-site" \
    --settings "KEY1=VALUE1" "KEY2=VALUE2"
```

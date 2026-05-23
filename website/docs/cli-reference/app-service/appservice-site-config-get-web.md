---
sidebar_position: 8
---

# appservice site config get-web
Gets the web configuration of a Web App (Microsoft.Web/sites/config/web).

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Site name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Get web config
```bash
$ topaz appservice site config get-web \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-site"
```

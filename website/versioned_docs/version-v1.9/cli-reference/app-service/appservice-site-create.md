---
sidebar_position: 3
---

# appservice site create
Creates or updates a Web App or Function App (Microsoft.Web/sites).

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Site name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region.
* `--kind` - (Optional) Site kind: app, functionapp, functionapp,linux. Defaults to app.
* `--plan` - (Optional) Resource ID of the App Service Plan to associate with this site.

## Examples

### Create a web app
```bash
$ topaz appservice site create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-site" \
    --location "westeurope"
```

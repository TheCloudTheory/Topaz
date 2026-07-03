---
sidebar_position: 7
---

# appservice site get
Gets a Web App or Function App.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Site name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Get a web app
```bash
$ topaz appservice site get \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-site"
```

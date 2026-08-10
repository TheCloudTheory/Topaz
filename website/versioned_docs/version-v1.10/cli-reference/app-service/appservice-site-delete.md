---
sidebar_position: 5
---

# appservice site delete
Deletes a Web App or Function App.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Site name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete a web app
```bash
$ topaz appservice site delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-site"
```

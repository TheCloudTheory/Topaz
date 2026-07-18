---
sidebar_position: 14
---

# appservice site config update-web
Updates the web configuration of a Web App (Microsoft.Web/sites/config/web). Only supplied fields are changed.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service Site name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `--linux-fx-version` - Linux runtime stack (e.g. DOTNETCORE|8.0).
* `--net-framework-version` - .NET Framework version (e.g. v8.0).
* `--always-on` - Enable Always On.
* `--ftps-state` - FTPS state (AllAllowed, FtpsOnly, Disabled).
* `--min-tls-version` - Minimum TLS version (e.g. 1.2).

## Examples

### Set always-on and Linux runtime
```bash
$ topaz appservice site config update-web \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-site" \
    --always-on true \
    --linux-fx-version "DOTNETCORE|8.0"
```

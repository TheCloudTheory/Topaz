---
sidebar_position: 1
---

# appservice site check-name
Checks if the provided App Service site name is globally available.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) App Service site name to check.

## Examples

### Check App Service site name
```bash
$ topaz appservice site check-name \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --name "my-webapp"
```

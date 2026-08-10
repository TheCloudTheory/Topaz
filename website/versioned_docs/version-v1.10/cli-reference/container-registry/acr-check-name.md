---
sidebar_position: 1
---

# acr check-name
Checks whether a container registry name is available.

## Options
* `-n, --name` - (Required) Registry name to check.
* `-s, --subscription-id` - (Required) Subscription ID.

## Examples

### Check registry name availability
```bash
$ topaz acr check-name \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --name "myregistry"
```

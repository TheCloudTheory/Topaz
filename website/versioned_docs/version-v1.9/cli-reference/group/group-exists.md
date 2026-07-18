---
sidebar_position: 1
---

# group exists
Checks whether a resource group exists.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Resource group name.

## Examples

### Check if a resource group exists
```bash
$ topaz group exists \
    --name "my-rg" \
    --subscription-id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A"
```

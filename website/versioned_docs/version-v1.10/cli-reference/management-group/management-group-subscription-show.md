---
sidebar_position: 12
---

# management-group subscription show
Shows a subscription association under a management group.

## Options
* `-g, --group-id` - (Required) (Required) Management group ID.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Show a subscription under a management group
```bash
$ topaz management-group subscription show --group-id "my-mg" --subscription-id "<guid>"
```

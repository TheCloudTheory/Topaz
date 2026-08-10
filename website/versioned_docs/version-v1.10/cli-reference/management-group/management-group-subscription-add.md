---
sidebar_position: 1
---

# management-group subscription add
Associates a subscription with a management group.

## Options
* `-g, --group-id` - (Required) (Required) Management group ID.
* `-s, --subscription-id` - (Required) (Required) Subscription ID to associate.

## Examples

### Associate a subscription
```bash
$ topaz management-group subscription add --group-id "my-mg" --subscription-id "<guid>"
```

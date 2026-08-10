---
sidebar_position: 9
---

# management-group subscription remove
Disassociates a subscription from a management group.

## Options
* `-g, --group-id` - (Required) (Required) Management group ID.
* `-s, --subscription-id` - (Required) (Required) Subscription ID to disassociate.

## Examples

### Remove a subscription from a management group
```bash
$ topaz management-group subscription remove --group-id "my-mg" --subscription-id "<guid>"
```

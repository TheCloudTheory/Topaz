---
sidebar_position: 2
---

# subscription create
Creates a new subscription.

## Options
* `-i, --id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Subscription display name.
* `-t, --tag` - Tags to assign to the subscription (key=value).

## Examples

### Create a subscription
```bash
$ topaz subscription create \
    --id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A" \
    --name "my-subscription"
```

---
sidebar_position: 2
---

# servicebus queue create
Creates or updates a queue in a Service Bus namespace.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --queue-name` - (Required) (Required) Queue name.
* `--namespace-name` - (Required) (Required) Service Bus namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Create a queue
```bash
$ topaz servicebus queue create \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --namespace-name "sblocal" \
    --queue-name "myqueue"
```

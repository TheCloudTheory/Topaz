---
sidebar_position: 4
---

# servicebus queue delete
Deletes a queue from a Service Bus namespace.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --queue-name` - (Required) (Required) Queue name.
* `--namespace-name` - (Required) (Required) Service Bus namespace name.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### Delete a queue
```bash
$ topaz servicebus queue delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --namespace-name "sblocal" \
    --queue-name "myqueue"
```

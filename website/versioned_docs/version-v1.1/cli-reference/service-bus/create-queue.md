---
sidebar_position: 3
---

# servicebus queue create

Creates a new Service Bus queue.

## Options
* `-n|--queue-name` - (Required) queue name
* `--namespace-name` - (Required) namespace name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Create a queue
```bash
$ topaz servicebus queue create \
    --resource-group rg-test \
    --namespace-name "sb-namespace" \
    --queue-name "queue-test"
```
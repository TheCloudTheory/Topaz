---
sidebar_position: 4
---

# servicebus queue delete

Deletes a Bus queue.

## Options
* `-n|--queue-name` - (Required) queue name
* `--namespace-name` - (Required) namespace name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Create a queue
```bash
$ topaz servicebus queue delete \
    --resource-group rg-test \
    --namespace-name "sb-namespace" \
    --queue-name "queue-test"
```
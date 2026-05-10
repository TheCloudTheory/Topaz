---
sidebar_position: 3
---

# eventhubs eventhub delete
Deletes an Event Hub.

## Options
* `-n, --name` - (Required) Event Hub name.
* `--namespace-name` - (Required) Event Hub namespace name.

## Examples

### Deletes Event Hub
```bash
$ topaz eventhubs eventhub delete \
    --namespace-name "sb-namespace" \
    --name "ehtest"
```

---
sidebar_position: 4
---

# eventhubs eventhub delete

Deletes Event Hub.

## Options
* `-n|--name` - (Required) hub name
* `--namespace-name` - (Required) namespace name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Delete a hub
```bash
$ topaz eventhubs eventhub delete \
    --resource-group rg-test \
    --namespace-name "sb-namespace" \
    --name "ehtest"
```
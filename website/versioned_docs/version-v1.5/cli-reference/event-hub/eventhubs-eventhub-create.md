---
sidebar_position: 1
---

# eventhubs eventhub create
Creates new Event Hub.

## Options
* `-n, --name` - (Required) hub name.
* `--namespace-name` - (Required) namespace name.
* `-g, --resource-group` - (Required) resource group name.
* `-s, --subscription-id` - (Required) subscription ID.

## Examples

### Creates Event Hub
```bash
$ topaz eventhubs eventhub create \
    --resource-group rg-test \
    --namespace-name "eh-namespace" \
    --name "hubtest" \
    --subscription-id "07CB2605-9C16-46E9-A2BD-0A8D39E049E8"
```

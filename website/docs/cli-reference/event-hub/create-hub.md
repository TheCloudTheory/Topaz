---
sidebar_position: 3
---

# eventhubs eventhub create

Creates new Event Hub.

## Options
* `-n|--name` - (Required) hub name
* `--namespace-name` - (Required) namespace name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Create a hub
```bash
$ topaz eventhubs eventhub create \
    --resource-group rg-test \
    --namespace-name "eh-namespace" \
    --name "hubtest"
```
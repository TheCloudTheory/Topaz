---
sidebar_position: 1
---

# eventhub namespace create

Creates a new Event Hub namespace.

## Options
* `-n|--name` - (Required) namespace name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Create a namespace
```bash
$ topaz eventhubs namespace create \
    --resource-group rg-test \
    --location "westeurope" \
    --name "eh-namespace"
```
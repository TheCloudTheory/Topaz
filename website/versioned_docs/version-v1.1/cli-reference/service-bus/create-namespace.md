---
sidebar_position: 1
---

# servicebus namespace create

Creates a new Service Bus namespace.

## Options
* `-n|--name` - (Required) namespace name
* `-g|--resource-group` - (Required) resource group name

## Examples

### Create a namespace
```bash
$ topaz servicebus namespace create \
    --resource-group rg-test \
    --name "sb-namespace"
```
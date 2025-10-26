---
sidebar_position: 2
---

# eventhubs namespace create
Creates new Event Hub namespace.

## Options
* `-n, --name` - (Required) hub name.
* `-g, --resource-group` - (Required) resource group name.
* `-l, --location` - (Required) Event Hub namespace location.
* `-s, --subscription-id` - (Required) subscription ID.

## Examples

### Creates Event Hub namespace
```bash
$ topaz eventhubs namespace create \
    --resource-group rg-test \
    --location "westeurope" \
    --name "eh-namespace" \
    --subscription-id "07CB2605-9C16-46E9-A2BD-0A8D39E049E8"
```

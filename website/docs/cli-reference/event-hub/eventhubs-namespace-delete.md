---
sidebar_position: 4
---

# eventhubs namespace delete
Deletes an Event Hub.

## Options
* `-n, --name` - (Required) Event Hub namespace name.
* `-g, --resource-group` - (Required) Event Hub namespace resource group name.
* `-s, --subscription-id` - (Required) Event Hub namespace subscription ID.

## Examples

### Deletes Event Hub
```bash
$ topaz eventhubs namespace delete \
    --name "sb-namespace" \
    --resource-group "rg" \
    --subscription-id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A"
```

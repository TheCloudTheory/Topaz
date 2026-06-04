---
sidebar_position: 9
---

# cosmosdb account list
Lists Azure Cosmos DB accounts in a resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - (Required) (Required) Resource group name.

## Examples

### List Cosmos DB accounts in a resource group
```bash
$ topaz cosmosdb account list \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local"
```

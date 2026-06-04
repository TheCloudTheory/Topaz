---
sidebar_position: 14
---

# cosmosdb account update
Updates tags on an Azure Cosmos DB account.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Cosmos DB account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `--tags` - (Optional) Space-separated tags in key=value format.

## Examples

### Update tags on a Cosmos DB account
```bash
$ topaz cosmosdb account update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-cosmos-account" \
    --tags "env=dev team=platform"
```

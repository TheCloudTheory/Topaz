---
sidebar_position: 14
---

# cosmosdb account regenerate-key
Regenerates an access key for an Azure Cosmos DB account.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Cosmos DB account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-k, --key-kind` - (Required) (Required) Key kind to regenerate: primary, secondary, primaryReadonly, secondaryReadonly.

## Examples

### Regenerate the primary key for a Cosmos DB account
```bash
$ topaz cosmosdb account regenerate-key \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --name "my-cosmos-account" \
    --key-kind "primary"
```

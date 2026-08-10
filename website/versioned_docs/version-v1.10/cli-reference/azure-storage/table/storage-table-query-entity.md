---
sidebar_position: 6
---

# storage table query-entity
Queries entities in a storage table with an optional OData filter.

## Options
* `-t, --table-name` - (Required) (Required) Table name.
* `--account-name` - (Required) (Required) Storage account name.
* `-f, --filter` - OData filter expression (e.g. "PartitionKey eq 'pk1'").
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Query all entities
```bash
$ topaz storage table query-entity \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --table-name "mytable"
```

### Query with filter
```bash
$ topaz storage table query-entity \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --table-name "mytable" \
    --filter "PartitionKey eq 'pk1'"
```

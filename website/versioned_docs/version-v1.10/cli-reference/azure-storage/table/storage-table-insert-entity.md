---
sidebar_position: 4
---

# storage table insert-entity
Inserts an entity into a storage table.

## Options
* `-t, --table-name` - (Required) (Required) Table name.
* `--account-name` - (Required) (Required) Storage account name.
* `-e, --entity` - (Required) (Required) JSON-encoded entity object with PartitionKey and RowKey.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Insert an entity
```bash
$ topaz storage table insert-entity \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --table-name "mytable" \
    --entity '{"PartitionKey":"pk1","RowKey":"rk1","Value":"hello"}'
```

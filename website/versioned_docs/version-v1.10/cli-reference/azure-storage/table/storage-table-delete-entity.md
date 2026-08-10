---
sidebar_position: 3
---

# storage table delete-entity
Deletes an entity from a storage table.

## Options
* `-t, --table-name` - (Required) (Required) Table name.
* `--account-name` - (Required) (Required) Storage account name.
* `-p, --partition-key` - (Required) (Required) Partition key of the entity.
* `-r, --row-key` - (Required) (Required) Row key of the entity.
* `--if-match` - ETag for conditional delete (defaults to * for unconditional).
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Delete an entity
```bash
$ topaz storage table delete-entity \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --table-name "mytable" \
    --partition-key "pk1" \
    --row-key "rk1"
```

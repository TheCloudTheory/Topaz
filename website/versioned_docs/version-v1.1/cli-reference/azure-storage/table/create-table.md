---
sidebar_position: 1
---

# storage table create

Creates a new table in Azure Storage.

## Options
* `-n|--name` - (Required) table name
* `--account-name` - (Required) storage account name

## Examples

### Create new table
```bash
$ topaz storage table create --account-name "salocal" --name "mydata"
```
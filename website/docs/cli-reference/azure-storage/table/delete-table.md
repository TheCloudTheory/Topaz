---
sidebar_position: 2
---

# storage table delete

Deletes a table from Azure Storage.

## Options
* `-n|--name` - (Required) table name
* `--account-name` - (Required) storage account name

## Examples

### Delete a table
```bash
topaz storage table delete --account-name "salocal" --name "mydata"
```
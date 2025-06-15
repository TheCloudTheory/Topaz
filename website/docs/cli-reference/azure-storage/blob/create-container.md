---
sidebar_position: 1
---

# storage container create

Creates a new blob container in Azure Storage.

## Options
* `-n|--name` - (Required) table name
* `--account-name` - (Required) storage account name

## Examples

### Create new container
```bash
$ topaz storage container create --account-name "salocal" --name "mydata"
```
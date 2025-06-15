---
sidebar_position: 2
---

# storage account delete

Deletes a storage account.

## Options
* `-n|--name` - (Required) storage account name

## Examples

### Delete a storage account
```bash
topaz storage account delete --name "salocal"
```

## Remarks
When storage account is deleted, all the data stored by its services (Table, Blob, Queue) will also be deleted.
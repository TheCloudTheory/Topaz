---
sidebar_position: 2
---

# group delete

Deletes a resource group.

## Options
* `-n|--name` - (Required) resource group name

## Examples

### Delete a resource group
```bash
$ topaz group delete --name "rg-local"
```

## Remarks
Deleting a resource group doesn't affect existing resources. This behaviour will change in the future.
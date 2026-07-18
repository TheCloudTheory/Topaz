---
sidebar_position: 10
---

# storage blob snapshot
Creates a read-only snapshot of a blob at the current time.

## Options
* `--account-name` - (Required) (Required) Storage account name.
* `-c, --container-name` - (Required) (Required) Container name.
* `-n, --name` - (Required) (Required) Blob name.
* `--lease-id` - Lease ID required if the blob has an active lease.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Create a blob snapshot
```bash
$ topaz storage blob snapshot \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --container-name "mycontainer" \
    --name "file.txt"
```

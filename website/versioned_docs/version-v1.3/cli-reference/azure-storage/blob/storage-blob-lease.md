---
sidebar_position: 5
---

# storage blob lease
Manages lease operations on a blob (acquire, renew, change, release, break).

## Options
* `--account-name` - (Required) (Required) Storage account name.
* `-c, --container-name` - (Required) (Required) Container name.
* `-n, --name` - (Required) (Required) Blob name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--action` - (Required) (Required) Lease action: acquire, renew, change, release, or break.
* `--lease-duration` - Lease duration in seconds (-1 for infinite).
* `--lease-id` - Existing lease ID (required for renew, change, release).
* `--proposed-lease-id` - Proposed lease ID (required for change action).
* `--lease-break-period` - Break period in seconds (used with break action).

## Examples

### Acquire a lease
```bash
$ topaz storage blob lease \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --container-name "mycontainer" \
    --name "file.txt" \
    --action "acquire" \
    --lease-duration 60
```

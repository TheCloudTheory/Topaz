---
sidebar_position: 7
---

# storage blob metadata update
Sets or replaces metadata key-value pairs on a blob.

## Options
* `--account-name` - (Required) (Required) Storage account name.
* `-c, --container-name` - (Required) (Required) Container name.
* `-n, --name` - (Required) (Required) Blob name.
* `--metadata` - Metadata key=value pairs (e.g. --metadata "env=prod" "owner=team").
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Set blob metadata
```bash
$ topaz storage blob metadata update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --container-name "mycontainer" \
    --name "file.txt" \
    --metadata "env=prod" "owner=team"
```

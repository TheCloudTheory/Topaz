---
sidebar_position: 4
---

# storage container metadata set
Sets metadata key-value pairs on a blob container.

## Options
* `-n, --name` - (Required) (Required) Container name.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--metadata` - Metadata key=value pairs (e.g. --metadata "env=prod" "owner=team").

## Examples

### Set container metadata
```bash
$ topaz storage container metadata set \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --name "mycontainer" \
    --metadata "env=prod" "owner=team"
```

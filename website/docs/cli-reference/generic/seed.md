---
sidebar_position: 5
---

# seed
Imports resources from a remote source.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-g, --resource-group` - Scope import to a single resource group.
* `--resource-type` - Scope import to a specific resource type (e.g. Microsoft.Storage/storageAccounts).
* `--dry-run` - Preview what would be imported without writing anything to Topaz.
* `--overwrite` - Replace resources that already exist in the emulator.

## Examples

### Import all resources from a subscription
```bash
$ topaz seed --subscription-id "00000000-0000-0000-0000-000000000001"
```

### Import resources from a specific resource group (dry run)
```bash
$ topaz seed \
    --subscription-id "00000000-0000-0000-0000-000000000001" \
    --resource-group "rg-production" \
    --dry-run
```

### Import only Storage Accounts and overwrite existing
```bash
$ topaz seed \
    --subscription-id "00000000-0000-0000-0000-000000000001" \
    --resource-type "Microsoft.Storage/storageAccounts" \
    --overwrite
```

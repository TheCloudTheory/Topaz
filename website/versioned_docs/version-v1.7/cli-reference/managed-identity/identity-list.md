---
sidebar_position: 3
---

# identity list
Lists user-assigned managed identities.

## Options
* `-g, --resource-group` - (Optional) resource group name
* `-s, --subscription-id` - (Required) subscription ID

## Examples

### Lists managed identities by resource group
```bash
$ topaz identity list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae \
    --resource-group "rg-local"
```

### Lists managed identities by subscription
```bash
$ topaz identity list --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae
```

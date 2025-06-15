---
sidebar_position: 3
---

# storage account keys list

List access keys for the given Storage Account.

## Options
* `-n|--account-name` - (Required) storage account name
* `-g|--resource-group` - (Required) resource group name

## Examples

### List access keys
```bash
$ topaz storage account keys list --account-name "salocal" --resource-group "rg-local"

{
    "keys": [
        {
            "keyName": "key1",
            "value": "AB329159310B3F30EAB558A1CBCBD5B336831F6D",
            "permissions": "Full",
            "creationTime": "2025-06-15T20:30:20.893635+02:00"
        },
        {
            "keyName": "key2",
            "value": "C9522EB61359C880E4ACC0F86EE61A9247617455",
            "permissions": "Full",
            "creationTime": "2025-06-15T20:30:20.899289+02:00"
        }
    ]
}
```

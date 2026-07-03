---
sidebar_position: 2
---

# configure set
Sets default values for the CLI.

## Options
* `-s, --subscription-id` - Default subscription ID.
* `-g, --resource-group` - Default resource group name
* `-l, --location` - Default location

## Examples

### Set default subscription, resource group, and location
```bash
$ topaz configure set \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "my-resource-group" \
    --location "eastus"
```

---
sidebar_position: 1
---

# group create
Creates a new resource group.

## Options
* `-n, --name` - (Required) (Required) Resource group name.
* `-l, --location` - (Required) (Required) Azure region for the resource group.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Create a resource group
```bash
$ topaz group create \
    --name "my-rg" \
    --location "eastus" \
    --subscription-id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A"
```

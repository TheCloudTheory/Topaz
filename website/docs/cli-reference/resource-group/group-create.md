---
sidebar_position: 1
---

# group create

Creates a new resource group.

## Options
* `-s|--subscription-id` - (Required) subscription ID
* `-n|--name` - (Required) resource group name
* `-l|--location` - (Required) resource group name

## Examples

### Create a resource group
```bash
$ topaz group create --subscription-id 36a28ebb-9370-46d8-981c-84efe02048ae --name "rg-local" --location "westeurope"
```
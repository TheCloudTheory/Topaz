---
sidebar_position: 2
---

# management-group create
Creates or updates a management group.

## Options
* `-n, --name` - (Required) (Required) Management group ID / name.
* `-d, --display-name` - Display name for the management group.
* `-p, --parent-id` - ARM ID or name of the parent management group.

## Examples

### Create a management group
```bash
$ topaz management-group create --name "my-mg" --display-name "My Management Group"
```

### Create a management group under a parent
```bash
$ topaz management-group create --name "child-mg" --display-name "Child MG" \
    --parent-id "/providers/Microsoft.Management/managementGroups/parent-mg"
```

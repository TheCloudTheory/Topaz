---
sidebar_position: 14
---

# management-group update
Updates a management group.

## Options
* `-n, --name` - (Required) (Required) Management group ID / name.
* `-d, --display-name` - New display name for the management group.
* `-p, --parent-id` - ID of the new parent management group.

## Examples

### Rename a management group
```bash
$ topaz management-group update --name "my-mg" --display-name "New Display Name"
```

### Re-parent a management group
```bash
$ topaz management-group update --name "my-mg" --parent-id "parent-mg"
```

---
sidebar_position: 3
---

# management-group hierarchy-settings create-or-update
Creates or updates the hierarchy settings for a management group.

## Options
* `-n, --name` - (Required) (Required) Management group ID / name.
* `--require-authorization` - Require authorization to create child management groups.
* `--default-management-group` - ID of the default management group for new subscriptions.

## Examples

### Create hierarchy settings
```bash
$ topaz management-group hierarchy-settings create-or-update --name "my-mg" --require-authorization
```

### Set a default management group
```bash
$ topaz management-group hierarchy-settings create-or-update --name "my-mg" --default-management-group "default-mg"
```

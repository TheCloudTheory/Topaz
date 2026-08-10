---
sidebar_position: 13
---

# management-group hierarchy-settings update
Updates the hierarchy settings for a management group.

## Options
* `-n, --name` - (Required) (Required) Management group ID / name.
* `--require-authorization` - Require authorization to create child management groups.
* `--default-management-group` - ID of the default management group for new subscriptions.

## Examples

### Enable authorization requirement
```bash
$ topaz management-group hierarchy-settings update --name "my-mg" --require-authorization true
```

### Change the default management group
```bash
$ topaz management-group hierarchy-settings update --name "my-mg" --default-management-group "new-default-mg"
```

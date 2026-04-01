---
sidebar_position: 6
---

# group update
Updates the tags of a resource group.

## Options
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --name` - (Required) (Required) Resource group name.
* `--tags` - Tags as a JSON object, e.g. `&#123;"env":"prod"&#125;`. Replaces existing tags.

## Examples

### Update tags on a resource group
```bash
$ topaz group update \
    --name "my-rg" \
    --subscription-id "6B1F305F-7C41-4E5C-AA94-AB937F2F530A" \
    --tags '{"env":"prod"}'
```

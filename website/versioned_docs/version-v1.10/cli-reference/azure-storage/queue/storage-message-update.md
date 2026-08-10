---
sidebar_position: 9
---

# storage message update
Updates the visibility timeout and/or content of a dequeued message.

## Options
* `-q, --queue-name` - (Required) (Required) Queue name.
* `-i, --message-id` - (Required) (Required) Message ID.
* `-r, --pop-receipt` - (Required) (Required) Pop receipt obtained when the message was dequeued.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--visibility-timeout` - New visibility timeout in seconds (0–604800). Default: 30.
* `-c, --content` - Updated message content. If omitted, content is preserved.

## Examples

### Update message visibility
```bash
$ topaz storage message update \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --queue-name "myqueue" \
    --message-id "<id>" \
    --pop-receipt "<receipt>" \
    --visibility-timeout 60
```

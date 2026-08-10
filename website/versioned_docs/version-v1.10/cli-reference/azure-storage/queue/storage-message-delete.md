---
sidebar_position: 5
---

# storage message delete
Deletes a message from a queue.

## Options
* `-q, --queue-name` - (Required) (Required) Queue name.
* `-i, --message-id` - (Required) (Required) Message ID.
* `-r, --pop-receipt` - (Required) (Required) Pop receipt obtained when the message was dequeued.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.

## Examples

### Delete a message
```bash
$ topaz storage message delete \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --queue-name "myqueue" \
    --message-id "<id>" \
    --pop-receipt "<receipt>"
```

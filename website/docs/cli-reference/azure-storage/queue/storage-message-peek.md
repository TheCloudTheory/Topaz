---
sidebar_position: 7
---

# storage message peek
Peeks at one or more messages in a queue without dequeuing them.

## Options
* `-q, --queue-name` - (Required) (Required) Queue name.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --num-messages` - Number of messages to peek (1–32). Default: 1.

## Examples

### Peek at up to 5 messages
```bash
$ topaz storage message peek \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --queue-name "myqueue" \
    --num-messages 5
```

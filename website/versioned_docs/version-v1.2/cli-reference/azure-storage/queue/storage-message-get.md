---
sidebar_position: 6
---

# storage message get
Dequeues one or more messages from a queue, hiding them for the visibility timeout duration.

## Options
* `-q, --queue-name` - (Required) (Required) Queue name.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `-n, --num-messages` - Number of messages to retrieve (1–32). Default: 1.
* `--visibility-timeout` - Seconds messages are hidden after dequeue (0–604800). Default: 30.

## Examples

### Dequeue up to 5 messages
```bash
$ topaz storage message get \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --queue-name "myqueue" \
    --num-messages 5
```

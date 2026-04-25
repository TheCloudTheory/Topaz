---
sidebar_position: 7
---

# storage message put
Enqueues a new message in a queue.

## Options
* `-q, --queue-name` - (Required) (Required) Queue name.
* `-c, --content` - (Required) (Required) Message content.
* `--account-name` - (Required) (Required) Storage account name.
* `-g, --resource-group` - (Required) (Required) Resource group name.
* `-s, --subscription-id` - (Required) (Required) Subscription ID.
* `--visibility-timeout` - Seconds the message is invisible after enqueue (0–604800). Default: 0.
* `--ttl` - Message time-to-live in seconds (1–604800). Default: 604800.

## Examples

### Enqueue a message
```bash
$ topaz storage message put \
    --subscription-id "00000000-0000-0000-0000-000000000000" \
    --resource-group "rg-local" \
    --account-name "salocal" \
    --queue-name "myqueue" \
    --content "Hello World"
```

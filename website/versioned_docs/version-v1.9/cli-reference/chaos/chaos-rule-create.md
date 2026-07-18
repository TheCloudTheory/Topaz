---
sidebar_position: 4
---

# chaos rule create
Creates a chaos fault rule.

## Options
* `--rule-id` - (Required) (Required) Unique ID for the rule.
* `--namespace` - (Required) (Required) Service namespace to match, e.g. Microsoft.Storage or * for all.
* `--fault-type` - (Required) (Required) Fault type: Timeout | TransientError | Throttle | ServiceUnavailable.
* `--rate` - (Required) (Required) Probability of injecting the fault (0.0–1.0).
* `--status-code` - (Optional) HTTP status code override (e.g. 429, 500, 503).

## Examples

### Create a throttle rule for Storage
```bash
$ topaz chaos rule create --rule-id throttle-storage --namespace Microsoft.Storage --fault-type Throttle --rate 0.5 --status-code 429
```

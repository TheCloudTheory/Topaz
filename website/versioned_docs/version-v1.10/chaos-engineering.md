---
sidebar_position: 19
description: Inject controllable faults into the Topaz router pipeline to test how your application handles Azure service failures — transient errors, throttling, and service unavailability — without touching real Azure.
keywords: [topaz chaos engineering, fault injection, azure resilience testing, transient errors local, throttling test, chaos mode azure emulator]
---

# Chaos Engineering

Topaz ships a built-in fault injection engine that lets you inject controlled failures into any emulated Azure endpoint. This lets you verify that your application handles transient errors, throttling, and service outages correctly — entirely on localhost, with no real Azure account.

## How it works

The fault injection engine sits inside the Topaz router, between the authorization check and the endpoint handler. When chaos mode is enabled and a matching fault rule fires, the router returns the configured fault response instead of calling the real endpoint handler. The application under test receives the fault as if Azure itself returned it.

```
Request
  → Authentication check
  → Provider registration check
  → 🎲 Chaos fault roll   ← injected here
  → Endpoint handler
  → Response
```

There are two controls:

- **Chaos mode** — a global on/off switch. No faults fire while chaos mode is off, regardless of what rules are configured.
- **Fault rules** — individual rules that define which service namespace to target, what type of fault to return, and how often to inject it.

Both are managed via the Topaz management API at `https://topaz.local.dev:8899/topaz/chaos/`.

:::note
Topaz management endpoints (`/topaz/...`) are always exempt from fault injection. This ensures that chaos management calls — enabling mode, creating rules — are never disrupted by the rules themselves.
:::

## Fault types

| Fault type | HTTP status | Additional behaviour |
|---|---|---|
| `TransientError` | `500 Internal Server Error` | Response body matches the standard Azure error format: `{"error":{"code":"InternalServerError","message":"..."}}` |
| `Throttle` | `429 Too Many Requests` | Includes a `Retry-After: 5` header |
| `Timeout` | `408 Request Timeout` | Response is delayed by 30 seconds before returning |
| `ServiceUnavailable` | `503 Service Unavailable` | Response is delayed by 60 seconds before returning |

## Fault rate

Each rule carries a `faultRate` field between `0.0` and `1.0` that controls what fraction of matching requests are faulted. A value of `0.1` means roughly 1 in 10 requests receives the fault; `1.0` means every request is faulted.

The rate is evaluated per-request with a random roll. This means a given request is never guaranteed to be faulted at rates below 1.0 — which mirrors real-world transient failure behaviour.

## Service namespace filtering

Each rule targets a specific Azure service namespace, or all services at once:

| `serviceNamespace` value | Behaviour |
|---|---|
| `*` | Applies to every non-Topaz endpoint |
| `Microsoft.KeyVault` | Applies only to Key Vault control-plane requests |
| `Microsoft.Storage` | Applies only to Storage control-plane requests |
| `Microsoft.ServiceBus` | Applies only to Service Bus control-plane requests |
| _(any ARM namespace)_ | Applies only to requests for that provider |

The namespace is matched against the `ProviderNamespace` declared by each endpoint. Data-plane endpoints (blob, queue, AMQP) do not carry a provider namespace and are not reached by namespace-scoped rules; use `"*"` to cover them.

## Managing chaos with the CLI

The `topaz` CLI is the preferred way to manage chaos mode and fault rules. All commands communicate with the running `topaz-host` instance.

### Chaos mode

```bash
topaz chaos enable    # turn fault injection on
topaz chaos disable   # turn fault injection off
topaz chaos status    # print the current enabled/disabled state
```

### Fault rules

**Create a rule**

```bash
topaz chaos rule create \
  --rule-id <id> \
  --namespace <serviceNamespace> \
  --fault-type <Timeout|TransientError|Throttle|ServiceUnavailable> \
  --rate <0.0–1.0>
```

| Option | Required | Description |
|---|---|---|
| `--rule-id` | Yes | Unique identifier for the rule — use any descriptive string |
| `--namespace` | Yes | Provider namespace to target, e.g. `Microsoft.KeyVault`, or `*` for all |
| `--fault-type` | Yes | One of `Timeout`, `TransientError`, `Throttle`, `ServiceUnavailable` |
| `--rate` | Yes | Probability of injecting the fault per request (`0.0`–`1.0`) |
| `--status-code` | No | Override the default HTTP status code for the fault |

**List, show, and delete**

```bash
topaz chaos rule list
topaz chaos rule show   --rule-id <id>
topaz chaos rule delete --rule-id <id>
```

**Enable or disable individual rules**

```bash
topaz chaos rule enable  --rule-id <id>
topaz chaos rule disable --rule-id <id>
```

Disabling a rule pauses it without removing it, so it can be re-enabled later in the same test session.

## Typical test workflow

1. Start Topaz and provision whatever resources your test needs.
2. Enable chaos mode: `topaz chaos enable`
3. Create one or more fault rules scoped to the service under test.
4. Run your application code and assert it handles the fault correctly (retries, fallbacks, error messages).
5. Clean up: `topaz chaos disable` and delete any rules you no longer need.

### Example: verify Key Vault retry logic

```bash
topaz chaos enable

topaz chaos rule create \
  --rule-id kv-throttle \
  --namespace Microsoft.KeyVault \
  --fault-type Throttle \
  --rate 0.5

# Run your application — expect it to handle 429 responses gracefully

topaz chaos rule delete --rule-id kv-throttle
topaz chaos disable
```

### Example: simulate a complete Storage outage

```bash
topaz chaos enable

topaz chaos rule create \
  --rule-id storage-down \
  --namespace Microsoft.Storage \
  --fault-type ServiceUnavailable \
  --rate 1.0

# Run your application — expect it to fall back gracefully

topaz chaos rule delete --rule-id storage-down
topaz chaos disable
```

## REST API reference

The CLI wraps the Topaz management REST API. You can call it directly if you prefer — for example, from test setup scripts that do not have access to the `topaz` binary.

**Chaos mode**

```http
POST https://topaz.local.dev:8899/topaz/chaos/enable
POST https://topaz.local.dev:8899/topaz/chaos/disable
GET  https://topaz.local.dev:8899/topaz/chaos/status
```

**Fault rules**

```http
PUT    https://topaz.local.dev:8899/topaz/chaos/rules/{ruleId}
GET    https://topaz.local.dev:8899/topaz/chaos/rules
GET    https://topaz.local.dev:8899/topaz/chaos/rules/{ruleId}
DELETE https://topaz.local.dev:8899/topaz/chaos/rules/{ruleId}
POST   https://topaz.local.dev:8899/topaz/chaos/rules/{ruleId}/enable
POST   https://topaz.local.dev:8899/topaz/chaos/rules/{ruleId}/disable
```

`PUT` body:

```json
{
  "serviceNamespace": "Microsoft.KeyVault",
  "faultType": "Throttle",
  "faultRate": 0.3
}
```

## Observability

Every injected fault is logged at **Information** level by `topaz-host`:

```
[ChaosProvider] Rule 'kv-throttle' triggered (faultType=Throttle, faultRate=0.5).
```

Use this to confirm faults are firing during a test run without having to inspect HTTP responses.

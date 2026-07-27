# Node.js AMQP smoke tests

Standalone AMQP compatibility smoke tests for Topaz Service Bus and Event Hub using the official `@azure/service-bus` and `@azure/event-hubs` Node.js packages.

These scripts form part of the Phase 1 AMQP baseline for the v1.6 decision track. They test whether `@azure/service-bus` and `@azure/event-hubs` (which use the `rhea` AMQP transport, not Python's pyamqp) are affected by the two AMQPNetLite protocol deviations:

1. Trailing null fields omitted in performatives.
2. Error composites encoded with 2 fields instead of 3.

## Prerequisites

- Node.js 20+
- A running Topaz host: `dotnet run --project Topaz.Host`
- Namespace and queue / hub already created (use `topaz` CLI or E2E fixture)
- DNS entries for the AMQP hostnames (add to `/etc/hosts`):

```
127.0.0.1 sb-test.servicebus.topaz.local.dev
127.0.0.1 test.eventhub.topaz.local.dev
```

## Setup

```bash
cd Topaz.Tests.NodeJS
npm install
```

## Run

```bash
# Service Bus smoke
TOPAZ_SB_CONNECTION_STRING="Endpoint=sb://sb-test.servicebus.topaz.local.dev:8889;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" \
TOPAZ_SB_QUEUE=queue-test \
npm run smoke:servicebus

# Event Hub smoke
TOPAZ_EH_CONNECTION_STRING="Endpoint=sb://test.eventhub.topaz.local.dev:8888;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" \
TOPAZ_EH_NAME=test \
npm run smoke:eventhub

# Both in sequence
npm run smoke:all
```

## Expected outcomes

| AMQPNetLite version | Expected result |
|---|---|
| 2.5.1 (current) | Document pass or fail — rhea is spec-lenient and may pass |
| 2.5.3 (Phase 2) | Must pass without any client-side workarounds |

Exit code 0 = PASS, exit code 1 = FAIL with error message printed to stderr.

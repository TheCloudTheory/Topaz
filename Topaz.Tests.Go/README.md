# Go AMQP smoke tests

Standalone AMQP compatibility smoke tests for Topaz Service Bus and Event Hub using the official Azure SDK for Go.

These programs form part of the Phase 1 AMQP baseline for the v1.6 decision track. They test whether the Go Azure SDK (which uses `go-amqp` as its AMQP transport) is affected by the two AMQPNetLite protocol deviations:

1. Trailing null fields omitted in performatives.
2. Error composites encoded with 2 fields instead of 3.

## Prerequisites

- Go 1.22+
- A running Topaz host: `dotnet run --project Topaz.Host`
- Namespace and queue / hub already created (use `topaz` CLI or E2E fixture)
- DNS entries for the AMQP hostnames (add to `/etc/hosts`):

```
127.0.0.1 sb-test.servicebus.topaz.local.dev
127.0.0.1 test.eventhub.topaz.local.dev
```

## Setup

```bash
cd Topaz.Tests.Go
go mod download
```

## Run

```bash
cd smoke

# Both Service Bus and Event Hub
go run .

# Service Bus only
TOPAZ_SMOKE_MODE=servicebus go run .

# Event Hub only
TOPAZ_SMOKE_MODE=eventhub go run .

# Custom connection strings
TOPAZ_SB_CONNECTION_STRING="Endpoint=sb://my-ns.servicebus.topaz.local.dev:8889;..." \
TOPAZ_SB_QUEUE=my-queue \
TOPAZ_SMOKE_MODE=servicebus \
go run .
```

## Expected outcomes

| AMQPNetLite version | Expected result |
|---|---|
| 2.5.1 (current) | Document pass or fail — go-amqp behaviour TBD |
| 2.5.3 (Phase 2) | Must pass without any client-side workarounds |

Exit code 0 = PASS, exit code 1 = FAIL with error message printed to stderr.

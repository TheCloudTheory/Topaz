---
sidebar_position: 3
---

# Service Bus

:::info[Azure REST API reference]
- Control plane (ARM): [Azure Service Bus REST API](https://learn.microsoft.com/en-us/rest/api/servicebus/)
- Data plane: AMQP 1.0 protocol served on ports 8889 (AMQP) and 5671 (AMQP/TLS)
:::

This page tracks which Azure Service Bus REST API operations are implemented in Topaz, split by control plane (ARM resource management) and data plane (AMQP messaging).

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

The control plane covers ARM operations available under `management.azure.com` — creating and managing namespaces, queues, topics, subscriptions, and rules.

### Namespaces

> [REST reference](https://learn.microsoft.com/en-us/rest/api/servicebus/)

| Operation | Status | Notes |
|-----------|--------|-------|
| Check Name Availability | ❌ | |
| Create Or Update | ✅ | `PUT .../namespaces/{namespaceName}` |
| Delete | ✅ | |
| Get | ✅ | |
| List | ❌ | Subscription-level listing not implemented |
| List By Resource Group | ✅ | |
| Update | ❌ | |
| List Keys | ❌ | |
| Regenerate Keys | ❌ | |
| Get Authorization Rule | ❌ | |
| List Authorization Rules | ❌ | |
| Create Or Update Authorization Rule | ❌ | |
| Delete Authorization Rule | ❌ | |

### Queues

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| List By Namespace | ✅ |
| List Keys | ❌ |
| Regenerate Keys | ❌ |
| Get Authorization Rule | ❌ |
| List Authorization Rules | ❌ |
| Create Or Update Authorization Rule | ❌ |
| Delete Authorization Rule | ❌ |

### Topics

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| List By Namespace | ✅ |
| List Keys | ❌ |
| Regenerate Keys | ❌ |
| Get Authorization Rule | ❌ |
| List Authorization Rules | ❌ |
| Create Or Update Authorization Rule | ❌ |
| Delete Authorization Rule | ❌ |

### Subscriptions

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | Via data-plane AMQP endpoint |
| Delete | ✅ | Via data-plane AMQP endpoint |
| Get | ✅ | Via data-plane AMQP endpoint |
| List By Topic | ❌ | |

### Rules

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List By Subscriptions | ❌ |

### Disaster Recovery Configs

| Operation | Status |
|-----------|--------|
| Break Pairing | ❌ |
| Create Or Update | ❌ |
| Delete | ❌ |
| Fail Over | ❌ |
| Get | ❌ |
| Get Authorization Rule | ❌ |
| List | ❌ |
| List Authorization Rules | ❌ |
| List Keys | ❌ |

### Migration Configs

| Operation | Status |
|-----------|--------|
| Complete Migration | ❌ |
| Create And Start Migration | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Revert | ❌ |

### Private Endpoint Connections

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |

---

## Data Plane

The data plane covers AMQP 1.0 messaging operations served on port **8889** (AMQP) and **5671** (AMQP/TLS) in Topaz. Entity management via the data plane is also supported through the AMQP management node, which is how MassTransit and similar libraries interact with Service Bus.

### AMQP Messaging

| Operation | Status | Notes |
|-----------|--------|-------|
| Send message to queue | ✅ | |
| Send message to topic | ✅ | |
| Receive message from queue | ✅ | |
| Receive message from topic subscription | ✅ | |
| Complete / Abandon / Dead-letter message | ✅ | |

### AMQP Entity Management (via management node)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create / Get queue | ✅ | Used by MassTransit |
| Create / Get topic | ✅ | Used by MassTransit |
| Create / Get subscription | ✅ | Used by MassTransit |
| Delete queue | ✅ | |
| Delete topic | ✅ | |
| Delete subscription | ✅ | |

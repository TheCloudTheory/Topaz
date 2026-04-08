---
sidebar_position: 4
---

# Event Hub

:::info[Azure REST API reference]
- Control plane (ARM): [Azure Event Hubs REST API · 2021-11-01](https://learn.microsoft.com/en-us/rest/api/eventhub/)
- Data plane: HTTP on port 8897 (HTTPS) and AMQP on port 8888
:::

This page tracks which Azure Event Hubs REST API operations are implemented in Topaz, split by control plane (ARM resource management) and data plane (event sending/receiving).

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

The control plane covers ARM operations available under `management.azure.com` — creating and managing namespaces and Event Hub instances.

### Namespaces

> [REST reference](https://learn.microsoft.com/en-us/rest/api/eventhub/namespaces?view=rest-eventhub-2021-11-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Delete | ✅ | |
| Get | ✅ | |
| List | ❌ | Subscription-level listing not implemented |
| List By Resource Group | ❌ | |
| Update | ❌ | |
| Check Name Availability | ❌ | |
| List Keys | ❌ | |
| Regenerate Keys | ❌ | |
| Get Authorization Rule | ❌ | |
| List Authorization Rules | ❌ | |
| Create Or Update Authorization Rule | ❌ | |
| Delete Authorization Rule | ❌ | |

### Event Hubs

> [REST reference](https://learn.microsoft.com/en-us/rest/api/eventhub/event-hubs?view=rest-eventhub-2021-11-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Delete | ❌ | |
| Get | ✅ | |
| List By Namespace | ❌ | |
| List Keys | ❌ | |
| Regenerate Keys | ❌ | |
| Get Authorization Rule | ❌ | |
| List Authorization Rules | ❌ | |
| Create Or Update Authorization Rule | ❌ | |

### Consumer Groups

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List By Event Hub | ❌ |

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

### Schema Groups

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List By Namespace | ❌ |

### Private Endpoint Connections

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |

---

## Data Plane

The data plane covers event sending and receiving, served on port **8897** (HTTPS) and port **8888** (AMQP) in Topaz.

### Event Publishing

| Operation | Status | Notes |
|-----------|--------|-------|
| Send events (HTTP) | ✅ | `POST /{eventHubPath}/messages` |
| Send events (AMQP) | ✅ | |

### Event Consuming

| Operation | Status |
|-----------|--------|
| Receive events (AMQP) | ✅ |
| Receive events (HTTP) | ❌ |

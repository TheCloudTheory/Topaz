---
sidebar_position: 20
---

# Event Grid

:::info[Azure REST API reference]
- Control plane (ARM): [Azure Event Grid REST API](https://learn.microsoft.com/en-us/rest/api/eventgrid/controlplane-version2022-06-15/)
- Data plane: HTTP on the resource manager port, `POST /api/events`
:::

This page tracks which Azure Event Grid REST API operations are implemented in Topaz, split by control plane (ARM resource management) and data plane (event publishing).

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

The control plane covers ARM operations available under `management.azure.com` — creating and managing namespaces, topics, system topics, and their event subscriptions.

### Namespaces

> [REST reference](https://learn.microsoft.com/en-us/rest/api/eventgrid/controlplane-version2022-06-15/namespaces)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Delete | ✅ | |
| Get | ✅ | |
| List By Resource Group | ✅ | |
| List By Subscription | ✅ | |
| Update | ✅ | |
| List Shared Access Keys | ✅ | |
| Regenerate Key | ✅ | |
| Check Name Availability | ❌ | |

### Topics

> [REST reference](https://learn.microsoft.com/en-us/rest/api/eventgrid/controlplane-version2022-06-15/topics)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Delete | ✅ | |
| Get | ✅ | |
| List By Resource Group | ✅ | |
| List By Subscription | ✅ | |
| Update | ✅ | |
| List Shared Access Keys | ✅ | |
| Regenerate Key | ✅ | |
| List Event Types | ✅ | |
| Check Name Availability | ❌ | |

### System Topics

> [REST reference](https://learn.microsoft.com/en-us/rest/api/eventgrid/controlplane-version2022-06-15/system-topics)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Delete | ✅ | |
| Get | ✅ | |
| List By Resource Group | ✅ | |
| List By Subscription | ✅ | |
| Update | ✅ | |

### Topic Event Subscriptions

> [REST reference](https://learn.microsoft.com/en-us/rest/api/eventgrid/controlplane-version2022-06-15/event-subscriptions)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT .../topics/{topicName}/eventSubscriptions/{eventSubscriptionName}` |
| Delete | ✅ | |
| Get | ✅ | |
| Update | ✅ | |
| List By Topic | ✅ | |
| Get Full Url | ✅ | |
| Get Delivery Attributes | ✅ | |

### System Topic Event Subscriptions

> [REST reference](https://learn.microsoft.com/en-us/rest/api/eventgrid/controlplane-version2022-06-15/system-topic-event-subscriptions)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT .../systemTopics/{topicName}/eventSubscriptions/{eventSubscriptionName}` |
| Delete | ✅ | |
| Get | ✅ | |
| Update | ✅ | |
| List By System Topic | ✅ | |
| Get Full Url | ✅ | |
| Get Delivery Attributes | ✅ | |

### Domains

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Domain Topics | ❌ |

### Partner Topics / Partner Namespaces / Event Channels

| Operation | Status |
|-----------|--------|
| All operations | ❌ |

---

## Data Plane

The data plane covers publishing events to a topic over HTTP.

| Operation | Status | Notes |
|-----------|--------|-------|
| Publish Events (EventGrid schema) | ✅ | `POST /api/events` |
| Publish Events (CloudEvents schema) | ✅ | Detected via `Content-Type` header |
| Publish Events to Namespace Topic (CloudEvents, HTTP) | ❌ | |
| Receive/Acknowledge/Release/Reject (namespace pull delivery) | ❌ | |

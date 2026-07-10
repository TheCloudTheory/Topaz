---
sidebar_position: 5
---

# Endpoints

Each Topaz service is reachable on a dedicated hostname and port. The hostname encodes the resource name so the same port can serve multiple instances simultaneously.

:::tip[`TopazResourceHelpers`]
If you're using .NET to interact the Topaz, the `Topaz.ResourceManager` NuGet package exposes `TopazResourceHelpers` with helper methods that build these URLs for you — use those instead of hardcoding the patterns below.
:::

## Fixed endpoints

Services that don't depend on a specific resource name share a single hostname.

| Service | Endpoint | Port |
|---|---|---|
| Resource Manager (ARM) | `https://topaz.local.dev` | **8899** |
| Entra ID / Graph API | `https://topaz.local.dev` | **8899** |

## Per-resource endpoints

The `{name}` placeholder is the resource name you chose when creating the resource.

### Storage

All four storage sub-services share a single port. The sub-service is identified by the subdomain prefix.

| Sub-service | Endpoint pattern | Port |
|---|---|---|
| Blob | `https://{account}.blob.storage.topaz.local.dev` | **8891** |
| Queue | `https://{account}.queue.storage.topaz.local.dev` | **8891** |
| Table | `https://{account}.table.storage.topaz.local.dev` | **8891** |

### Key Vault

| Endpoint pattern | Port |
|---|---|
| `https://{vaultName}.vault.topaz.local.dev` | **8898** |

### Cosmos DB

| Endpoint pattern | Port |
|---|---|
| `https://{accountName}.documents.topaz.local.dev` | **8895** |

### App Configuration

| Endpoint pattern | Port |
|---|---|
| `https://{storeName}.azconfig.topaz.local.dev` | **8893** |

### Service Bus

Service Bus uses AMQP, not HTTPS. The scheme in connection strings is `sb://`.

| Use-case | Endpoint pattern | Port |
|---|---|---|
| Default (AMQP, no TLS) | `sb://{namespace}.servicebus.topaz.local.dev` | **8889** |
| TLS (MassTransit, etc.) | `sb://{namespace}.servicebus.topaz.local.dev` | **5671** |
| Management / HTTP | `sb://{namespace}.servicebus.topaz.local.dev` | **8887** |

### Event Hub

Event Hub also uses AMQP for the data plane.

| Plane | Endpoint pattern | Port |
|---|---|---|
| Data plane (AMQP) | `sb://{namespace}.eventhub.topaz.local.dev` | **8888** |
| Control plane (HTTP) | `https://topaz.local.dev` | **8897** |

### Container Registry

| Endpoint pattern | Port |
|---|---|
| `https://{registryName}.cr.topaz.local.dev` | **8892** |

### App Service

| Sub-service | Endpoint pattern | Port |
|---|---|---|
| Default hostname | `https://{siteName}.azurewebsites.topaz.local.dev` | **8899** |
| Kudu (SCM) | `https://{siteName}.scm.azurewebsites.topaz.local.dev` | **8896** |

### Log Analytics (ingestion)

The ingestion endpoint uses the workspace **Customer ID** (a GUID), not the workspace name. Retrieve it from the workspace properties after creation.

| Endpoint pattern | Port |
|---|---|
| `https://{workspaceCustomerId}.ods.opinsights.topaz.local.dev` | **8899** |

This endpoint shares port 8899 with ARM; the Topaz router dispatches it by the `ods.opinsights` subdomain label.

## Port summary

| Port | Service(s) |
|---|---|
| **8887** | Service Bus – management / HTTP |
| **8888** | Event Hub – AMQP |
| **8889** | Service Bus – AMQP (no TLS) |
| **8891** | Storage – Blob, Queue, Table |
| **8892** | Container Registry |
| **8893** | App Configuration |
| **8895** | Cosmos DB |
| **8896** | App Service Kudu |
| **8897** | Event Hub – HTTP control plane |
| **8898** | Key Vault |
| **8899** | ARM, Entra ID, App Service, Log Analytics ingestion |
| **5671** | Service Bus – AMQPS (TLS) |
| **44380** | Built-in HTTP CONNECT proxy |

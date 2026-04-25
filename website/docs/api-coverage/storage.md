---
sidebar_position: 5
---

# Storage

:::info[Azure REST API reference]
- Control plane (ARM): [Storage Resource Provider REST API · 2023-01-01](https://learn.microsoft.com/en-us/rest/api/storagerp/)
- Data plane – Blob: [Azure Blob Storage REST API](https://learn.microsoft.com/en-us/rest/api/storageservices/blob-service-rest-api)
- Data plane – Table: [Azure Table Storage REST API](https://learn.microsoft.com/en-us/rest/api/storageservices/table-service-rest-api)
:::

This page tracks which Azure Storage REST API operations are implemented in Topaz, split by control plane (ARM resource management) and data plane (Blob Storage on port 8891, Table Storage on port 8890).

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

The control plane covers ARM operations available under `management.azure.com` — creating and managing storage accounts.

### Storage Accounts

> [REST reference](https://learn.microsoft.com/en-us/rest/api/storagerp/storage-accounts?view=rest-storagerp-2023-01-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | Via PUT (CreateOrUpdate) |
| Delete | ✅ | |
| Get Properties | ✅ | |
| List | ✅ | `GET /subscriptions/{subscriptionId}/providers/Microsoft.Storage/storageAccounts` |
| List By Resource Group | ✅ | |
| List Keys | ✅ | `POST .../listKeys` |
| Check Name Availability | ✅ | `POST /subscriptions/{subscriptionId}/providers/Microsoft.Storage/checkNameAvailability` |
| Update | ✅ | `PATCH .../storageAccounts/{accountName}` |
| Regenerate Key | ✅ | `POST .../regenerateKey` |
| List Account SAS | ✅ | `POST .../ListAccountSas` |
| List Service SAS | ✅ | `POST .../ListServiceSas` |
| Failover | ❌ | |
| Restore Blob Ranges | ❌ | |
| Revoke User Delegation Keys | ❌ | |
| Abort Hierarchical Namespace Migration | ❌ | |
| Hierarchical Namespace Migration | ❌ | |
| Customer Initiated Migration | ❌ | |
| Get Customer Initiated Migration | ❌ | |

---

## Data Plane — Blob Storage

Blob Storage is served on port **8891** (HTTP) in Topaz.

### Containers

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Container | ✅ | `PUT /{containerName}?restype=container` |
| Get Container | ✅ | |
| Delete Container | ✅ | |
| List Containers | ✅ | `GET /` |
| Set Container Metadata | ✅ | `PUT /{containerName}?restype=container&comp=metadata` |
| Get Container Metadata | ✅ | `GET /{containerName}?restype=container&comp=metadata` |
| Get Container ACL | ✅ | `GET /{containerName}?restype=container&comp=acl` |
| Set Container ACL | ✅ | `PUT /{containerName}?restype=container&comp=acl` |
| Lease Container | ✅ | `PUT /{containerName}?restype=container&comp=lease` — acquire, renew, change, release, break |

### Blobs

| Operation | Status | Notes |
|-----------|--------|-------|
| Put Blob | ✅ | Upload blob |
| Get Blob | ✅ | `GET /{containerName}/{blobName}` — returns blob content with Content-Type, Content-Length, ETag, x-ms-blob-type headers |
| Delete Blob | ✅ | |
| Head Blob | ✅ | |
| Set Blob Metadata | ✅ | `PUT /{containerName}/{blobName}?comp=metadata` |
| Get Blob Metadata | ✅ | `GET /{containerName}/{blobName}?comp=metadata` |
| Get Blob Properties | ✅ | `HEAD /{containerName}/{blobName}` — returns Content-Type, Content-Length, ETag, Last-Modified, x-ms-blob-type, x-ms-creation-time, x-ms-meta-* |
| Set Blob Properties | ✅ | `PUT /{containerName}/{blobName}?comp=properties` |
| List Blobs | ✅ | `GET /{containerName}` |
| Copy Blob | ✅ | `PUT /{containerName}/{blobName}` with `x-ms-copy-source` header; synchronous within-emulator copy |
| Put Block | ✅ | `PUT /{containerName}/{blobName}?comp=block&blockid={blockId}` — stages a block for later commit via Put Block List |
| Put Block List | ✅ | `PUT /{containerName}/{blobName}?comp=blocklist` — assembles staged blocks into a committed blob |
| Get Block List | ✅ | `GET /{containerName}/{blobName}?comp=blocklist` — `blocklisttype` supports `committed`, `uncommitted`, `all` |
| Put Page | ✅ | `PUT /{containerName}/{blobName}?comp=page` — supports `x-ms-page-write: update` (write) and `clear` (zero-fill); range must be 512-byte aligned |
| Get Page Ranges | ✅ | `GET /{containerName}/{blobName}?comp=pagelist` — supports `Range`/`x-ms-range` filtering and returns Azure-compatible `PageList` XML |
| Lease Blob | ✅ | `PUT /{containerName}/{blobName}?comp=lease` — acquire, renew, change, release, break |
| Snapshot Blob | ✅ | |
| Undelete Blob | ✅ | |

---

## Data Plane — Table Storage

Table Storage is served on port **8890** (HTTPS) in Topaz.

### Service

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Table Service Properties | ✅ | `GET /` |
| Set Table Service Properties | ✅ | `PUT /?restype=service&comp=properties` |
| Get Table Service Stats | ✅ | `GET /?restype=service&comp=stats` |
| Preflight Table Request | ✅ | `OPTIONS /{resourcePath}` |

### Tables

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Table | ✅ | `POST /Tables` |
| Delete Table | ✅ | `DELETE /Tables('{tableName}')` |
| Query Tables | ✅ | `GET /Tables` |
| Get Table | ✅ | `GET /Tables('{tableName}')` |
| Get Table ACL | ✅ | `GET /{tableName}?comp=acl` |
| Set Table ACL | ✅ | `PUT /{tableName}?comp=acl` |

### Entities

| Operation | Status | Notes |
|-----------|--------|-------|
| Insert Entity | ✅ | `POST /{tableName}` |
| Get Entity | ✅ | `GET /{tableName}(PartitionKey='{pk}',RowKey='{rk}')` |
| Upsert Entity (InsertOrReplace) | ✅ | `PUT /{tableName}(...)` |
| Merge Entity (InsertOrMerge) | ✅ | `PATCH` |
| Delete Entity | ✅ | `DELETE /{tableName}(PartitionKey='{pk}',RowKey='{rk}')` |
| Query Entities | ✅ | `GET /{tableName}` |

---

## Data Plane — Queue Storage

Queue Storage is **partially implemented** in Topaz.

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Queue | ✅ | |
| Delete Queue | ✅ | |
| List Queues | ✅ | |
| Send Message (Enqueue) | ✅ | `POST /{queue-name}/messages` — create/enqueue a message |
| Get Messages (Dequeue) | ✅ | `GET /{queue-name}/messages` — retrieve messages with visibility timeout |
| Peek Messages | ❌ | `GET /{queue-name}/messages?peekonly=true` — retrieve without hiding |
| Delete Message | ❌ | `DELETE /{queue-name}/messages/{messageId}` |
| Update Message | ✅ | `PUT /{queue-name}/messages/{messageId}` — update visibility timeout and/or content |

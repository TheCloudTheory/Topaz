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
| List Keys | ✅ | `POST .../listKeys` — note: `az storage account show-connection-string` returns 404; use `--connection-string` explicitly in Azure CLI commands (see Known Limitations) |
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

### Blob Service

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Blob Service Stats | ✅ | `GET /?restype=service&comp=stats` — secondary endpoint only; returns 403 for non-RA-GRS accounts |
| List Containers (secondary) | ✅ | All blob read operations (List Containers, Get/List Blobs, etc.) are served from the primary data store when the request arrives on the `{account}-secondary.*` endpoint of an RA-GRS/RAGZRS account |
| Get Blob Service Properties | ✅ | `GET /?restype=service&comp=properties` — includes static website configuration |
| Set Blob Service Properties | ✅ | `PUT /?restype=service&comp=properties` |
| Generate User Delegation Key | ✅ | `POST /?restype=service&comp=userdelegationkey` — requires Bearer auth; key bytes derived deterministically from account key + caller OID/TID |

---

## Data Plane — Table Storage

Table Storage is served on port **8890** (HTTPS) in Topaz.

### Service

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Table Service Properties | ✅ | `GET /` |
| Set Table Service Properties | ✅ | `PUT /?restype=service&comp=properties` |
| Get Table Service Stats | ✅ | `GET /?restype=service&comp=stats` — secondary endpoint only; returns 403 for non-RA-GRS accounts |
| List/Read Tables & Entities (secondary) | ✅ | All table read operations (Query Tables, Get/Query Entities, etc.) are served from the primary data store when the request arrives on the `{account}-secondary.*` endpoint of an RA-GRS/RAGZRS account |
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
| Query Entities | ✅ | `GET /{tableName}` — supports `$filter` (OData v3: `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `and`, `or`, `not`; string, int32, int64, bool, datetime, guid literals), `$select`, `$top`, and server-side paging via `NextPartitionKey`/`NextRowKey` continuation headers |

---

## Data Plane — Queue Storage

Queue Storage is served on port **8893** (HTTPS) in Topaz.

### Service

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Queue Service Properties | ✅ | `GET /?restype=service&comp=properties` |
| Set Queue Service Properties | ✅ | `PUT /?restype=service&comp=properties` |
| Get Queue Service Stats | ✅ | `GET /?restype=service&comp=stats` — secondary endpoint only; returns 403 for non-RA-GRS accounts |
| List/Read Queues (secondary) | ✅ | All queue read operations (List Queues, Get Queue Metadata, Peek/Get Messages, etc.) are served from the primary data store when the request arrives on the `{account}-secondary.*` endpoint of an RA-GRS/RAGZRS account |

### Queues

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Queue | ✅ | `PUT /{queue-name}` |
| Delete Queue | ✅ | `DELETE /{queue-name}` |
| List Queues | ✅ | `GET /?comp=list` |
| Get Queue Metadata | ✅ | `GET /{queue-name}?comp=metadata` |
| Set Queue Metadata | ✅ | `PUT /{queue-name}?comp=metadata` |
| Get Queue ACL | ✅ | `GET /{queue-name}?comp=acl` |
| Set Queue ACL | ✅ | `PUT /{queue-name}?comp=acl` |

### Messages

| Operation | Status | Notes |
|-----------|--------|-------|
| Send Message (Enqueue) | ✅ | `POST /{queue-name}/messages` |
| Get Messages (Dequeue) | ✅ | `GET /{queue-name}/messages` — retrieve with visibility timeout |
| Peek Messages | ✅ | `GET /{queue-name}/messages?peekonly=true` — retrieve without hiding |
| Delete Message | ✅ | `DELETE /{queue-name}/messages/{messageId}?popreceipt={popReceipt}` |
| Update Message | ✅ | `PUT /{queue-name}/messages/{messageId}` — update visibility timeout and/or content |
| Clear Messages | ✅ | `DELETE /{queue-name}/messages` |

---

## Service SAS Authentication

Topaz validates [Service SAS](https://learn.microsoft.com/en-us/rest/api/storageservices/create-service-sas) tokens on all three data-plane services. The signature is verified using HMAC-SHA256 with the storage account key. Stored access policies (`si=` parameter) are resolved from the persisted ACL of each resource. Permission-letter enforcement (`sp=`) validates that the HTTP method of the incoming request is covered by the permission letters in the token (e.g. `r`→GET/HEAD, `w`→PUT, `d`→DELETE, `a`→POST, `u`→PUT/MERGE). Mismatched requests return 403 `AuthorizationPermissionMismatch`.

| Service | SAS resource type (`sr=`) | Status | Notes |
|---------|--------------------------|--------|-------|
| Blob | Container (`c`) | ✅ | StringToSign: 16 fields including `sr`, `si`, `sip`, `spr`, response header overrides |
| Blob | Blob (`b`) | ✅ | Full blob-level SAS (read, write, delete, create, add) |
| Blob | Stored access policy (`si=`) | ✅ | Policy resolved from `.container-acl.xml`; expiry / permissions merged from stored policy |
| Queue | Queue (`q`) | ✅ | StringToSign: 8 fields; add/process/read/update permissions |
| Queue | Stored access policy (`si=`) | ✅ | Policy resolved from `.acl.xml` |
| Table | Table (`t`) | ✅ | StringToSign: 12 fields including `spk`/`srk`/`epk`/`erk` row-range fields |
| Table | Stored access policy (`si=`) | ✅ | Policy resolved from `acl/{policyId}.xml` |

:::caution[Known Limitations]

- **`az storage account show-connection-string` returns 404.** The Azure CLI `show-connection-string` command resolves the storage account key via an internal ARM call that Topaz does not currently handle. Workaround: retrieve the connection string with `az storage account show-connection-string --name <account> --resource-group <rg> --query connectionString -o tsv` — if that also returns 404, pass credentials explicitly using `--connection-string`, `--account-key`, or `--sas-token` on every `az storage` data-plane command.

:::

---

## Account SAS Authentication

Topaz validates [Account SAS](https://learn.microsoft.com/en-us/rest/api/storageservices/create-account-sas) tokens on all three data-plane services. The signature is verified using HMAC-SHA256 with the storage account key, with full support for the `sv`, `ss`, `srt`, `sp`, `se`, `st`, `sip`, `spr`, and `ses` parameters.

Detection: Account SAS tokens are identified by the simultaneous presence of `sv=`, `sig=`, `ss=`, and `srt=` query parameters. This distinguishes them from Service SAS tokens (which lack `ss=` and `srt=`).

| Service | Resource type (`srt=`) | Status | Notes |
|---------|------------------------|--------|-------|
| Blob (`ss=b`) | Service (`s`) | ✅ | Service-level operations (list containers, get/set service properties) |
| Blob (`ss=b`) | Container (`c`) | ✅ | Container-level operations (list blobs, create/delete container) |
| Blob (`ss=b`) | Object (`o`) | ✅ | Blob-level operations (get, put, delete blob) |
| Queue (`ss=q`) | Service (`s`) | ✅ | Service-level operations (list queues, get/set service properties) |
| Queue (`ss=q`) | Container (`c`) | ✅ | Queue-level operations (create/delete queue, get metadata) |
| Queue (`ss=q`) | Object (`o`) | ✅ | Message-level operations (send, receive, delete, update messages) |
| Table (`ss=t`) | Service (`s`) | ✅ | Service-level operations (get/set service properties) |
| Table (`ss=t`) | Container (`c`) | ✅ | Table-level operations (query tables, create/delete table) |
| Table (`ss=t`) | Object (`o`) | ✅ | Entity-level operations (query, insert, update, delete entities) |

The `StringToSign` format follows the spec:
- For `sv < 2020-12-06`: `accountName\nsp\nss\nsrt\nst\nse\nsip\nspr\nsv\n`
- For `sv >= 2020-12-06`: `accountName\nsp\nss\nsrt\nst\nse\nsip\nspr\nsv\nses\n` (adds signed encryption scope)

:::caution[Known Limitations]

- **HTTP method enforcement (`sp=`)**: Permission letters are validated against the HTTP method of the request (e.g. `r`→GET, `w`→PUT, `d`→DELETE, `a`→POST add, `p`→GET process). Enforcement is complete for standard CRUD operations.
- **Encryption scope (`ses=`)**: The `ses` field is included in the `StringToSign` for versions ≥ 2020-12-06 but the encryption scope is not applied to storage operations.

:::

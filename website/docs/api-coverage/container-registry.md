---
sidebar_position: 1
---

# Container Registry

:::info[Azure REST API reference]
[Azure Container Registry REST API · 2025-11-01](https://learn.microsoft.com/en-us/rest/api/containerregistry/?view=rest-container-registry-2025-11-01)
:::

This page tracks which Azure Container Registry REST API operations are implemented in Topaz, split by control plane (ARM resource management) and data plane (OCI / Docker Registry HTTP API).

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

The control plane covers ARM operations available under `management.azure.com` — creating and managing registry resources, replications, webhooks, etc.

### Cache Rules

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/cache-rules?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Update | ❌ |

### Connected Registries

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/connected-registries?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Deactivate | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Update | ❌ |

### Credential Sets

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/credential-sets?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Update | ❌ |

### Operations

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/operations?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| List | ❌ |

### Private Endpoint Connections

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/private-endpoint-connections?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |

### Registries

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/registries?view=rest-container-registry-2025-11-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Check Name Availability | ✅ | |
| Create | ✅ | Implemented via PUT (CreateOrUpdate) |
| Delete | ✅ | |
| Generate Credentials | ✅ | |
| Get | ✅ | |
| Get Private Link Resource | ❌ | |
| Import Image | ❌ | |
| List | ✅ | Lists all registries under a subscription |
| List By Resource Group | ✅ | |
| List Credentials | ✅ | |
| List Private Link Resources | ❌ | |
| List Usages | ✅ | |
| Regenerate Credential | ✅ | |
| Update | ✅ | PATCH |

### Replications

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/replications?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Update | ❌ |

### Scope Maps

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/scope-maps?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Update | ❌ |

### Tokens

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/tokens?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Update | ❌ |

### Webhooks

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry/webhooks?view=rest-container-registry-2025-11-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| Get Callback Config | ❌ |
| List | ❌ |
| List Events | ❌ |
| Ping | ❌ |
| Update | ❌ |

---

## Data Plane

The data plane covers the [OCI Distribution Spec](https://github.com/opencontainers/distribution-spec/blob/main/spec.md) / Docker Registry HTTP API v2, served from the registry's own hostname (e.g. `<registry>.cr.topaz.local.dev:8892`).

### Authentication

> [ACR OAuth2 token exchange docs](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-authentication?tabs=azure-cli)

| Operation | Status | Notes |
|-----------|--------|-------|
| `GET /v2/` (challenge) | ✅ | Returns 401 Bearer challenge; accepts Basic (admin creds) or Bearer (JWT) |
| `POST /oauth2/exchange` | ✅ | Exchanges AAD access token for ACR refresh token |
| `GET /oauth2/token` | ✅ | Fetch repository-scoped Bearer access token (Docker daemon flow) |
| `POST /oauth2/token` | ✅ | Exchange refresh token for scoped repository access token |

### Manifests

| Operation | Status | Notes |
|-----------|--------|-------|
| Get | ✅ | `GET /v2/{name}/manifests/{reference}` — returns stored manifest JSON with original Content-Type |
| Put | ✅ | `PUT /v2/{name}/manifests/{reference}` — stores by tag and by digest |
| Delete | ✅ | `DELETE /v2/{name}/manifests/{reference}` — deletes by tag or digest; also removes digest-indexed copy |
| Check existence | ✅ | `HEAD /v2/{name}/manifests/{reference}` — returns 200 + Docker-Content-Digest + Content-Type + Content-Length when manifest exists |

### Blobs

| Operation | Status | Notes |
|-----------|--------|-------|
| Get | ✅ | `GET /v2/{name}/blobs/{digest}` — returns raw blob bytes with Content-Length |
| Check existence | ✅ | `HEAD /v2/{name}/blobs/{digest}` — returns 200 + Content-Length when blob exists |
| Delete | ❌ | |
| Initiate upload | ✅ | `POST /v2/{name}/blobs/uploads/` — returns session UUID |
| Upload (monolithic) | ✅ | `PUT /v2/{name}/blobs/uploads/{uuid}?digest=...` with body |
| Upload (chunked) | ✅ | `PATCH /v2/{name}/blobs/uploads/{uuid}` then `PUT` to finalize |

### Tags

| Operation | Status | Notes |
|-----------|--------|-------|
| List | ✅ | `GET /v2/{name}/tags/list` (OCI) and `GET /acr/v1/{name}/_tags` (ACR data-plane) — supports `n` and `last` pagination params |

### Repositories

| Operation | Status | Notes |
|-----------|--------|-------|
| List | ✅ | `GET /v2/_catalog` and `GET /acr/v1/_catalog` — returns `{"repositories":[...]}` sorted; supports `n` and `last` pagination params |

---

## Tasks API (planned for 1.5-beta)

:::info[Azure REST API reference]
[Azure Container Registry Tasks REST API · 2019-04-01](https://learn.microsoft.com/en-us/rest/api/container-registry-tasks/operation-groups?view=rest-container-registry-tasks-2019-04-01)
:::

This section tracks task-oriented ARM operations for building and running container images via triggers and pipelines.

### Registries (Tasks)

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry-tasks/registries?view=rest-container-registry-tasks-2019-04-01)

| Operation | Status |
|-----------|--------|
| Get Build Source Upload Url | ❌ |
| Schedule Run | ❌ |

### Runs

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry-tasks/runs?view=rest-container-registry-tasks-2019-04-01)

| Operation | Status |
|-----------|--------|
| Cancel | ❌ |
| Get | ❌ |
| Get Log Sas Url | ❌ |
| List | ❌ |
| Update | ❌ |

### Tasks

> [REST reference](https://learn.microsoft.com/en-us/rest/api/container-registry-tasks/tasks?view=rest-container-registry-tasks-2019-04-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| Get Details | ❌ |
| List | ❌ |
| Update | ❌ |

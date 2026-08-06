---
sidebar_position: 1
---

# API Management

:::info[Azure REST API reference]
[Azure API Management REST API](https://learn.microsoft.com/en-us/rest/api/apimanagement/)
:::

This page tracks which Azure API Management REST API operations are implemented in Topaz.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

The control plane covers ARM operations available under `management.azure.com` — creating and managing API Management service instances.

### Services

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/api-management-service)

| Operation | Status |
|-----------|--------|
| Check Name Availability | ✅ |
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| Get Deleted | ✅ |
| List | ✅ |
| List By Resource Group | ✅ |
| Update | ✅ |

---

## Data Plane

### APIs

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/api)

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| Get Entity Tag | ✅ |
| List By Service | ✅ |
| List Revisions | ✅ |
| Update | ✅ |

### Backends

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/backend)

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| Get Entity Tag | ✅ |
| List By Service | ✅ |
| Reconnect | ✅ |
| Update | ✅ |

### Policies

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/policy)

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| Get Entity Tag | ✅ |
| List By Service | ✅ |

### Portal Settings — Sign-In

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/sign-in-settings)

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Get | ✅ |
| Get Entity Tag | ✅ |
| Update | ✅ |

### Portal Settings — Sign-Up

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/sign-up-settings)

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Get | ✅ |
| Get Entity Tag | ✅ |
| Update | ✅ |

### Products

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/product)

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| Get Entity Tag | ✅ |
| List By Service | ✅ |
| Update | ✅ |

### Product APIs

> [REST reference](https://learn.microsoft.com/en-us/rest/api/apimanagement/product-api)

| Operation | Status |
|-----------|--------|
| Check Entity Exists | ✅ |
| Create Or Update | ✅ |
| Delete | ✅ |
| List By Product | ✅ |

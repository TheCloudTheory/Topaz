---
sidebar_position: 17
---

# App Configuration

:::info[Azure REST API reference]
- Control plane: [App Configuration REST API · 2024-05-01](https://learn.microsoft.com/en-us/rest/api/appconfiguration/configuration-stores?view=rest-appconfiguration-2024-05-01)
- Data plane: [Azure App Configuration data plane REST API](https://learn.microsoft.com/en-us/azure/azure-app-configuration/rest-api)
:::

This page tracks which Azure App Configuration REST API operations are implemented in Topaz.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane (`Microsoft.AppConfiguration/configurationStores`)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Get | ✅ | |
| Update | ✅ | PATCH |
| Delete | ✅ | |
| List By Resource Group | ✅ | |
| List By Subscription | ✅ | |
| List Keys | ✅ | |
| Regenerate Key | ✅ | |
| Check Name Availability | ❌ | |
| List Replicas | ✅ | |
| Create Replica | ✅ | |
| Delete Replica | ✅ | |
| Get Replica | ✅ | |

### Deleted Stores

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Deleted Store | ✅ | |
| List Deleted Stores | ✅ | |
| Purge Deleted Store | ✅ | |

---

## Data Plane

### Key-Values

> [REST reference](https://learn.microsoft.com/en-us/azure/azure-app-configuration/rest-api-key-value)

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Key-Value | ✅ | `GET /kv/{key}` — supports label filter |
| Set Key-Value | ✅ | `PUT /kv/{key}` |
| Delete Key-Value | ✅ | `DELETE /kv/{key}` |
| List Key-Values | ✅ | `GET /kv` — supports `key`, `label`, `$select`, and `After` filters |

### Locks

> [REST reference](https://learn.microsoft.com/en-us/azure/azure-app-configuration/rest-api-locks)

| Operation | Status | Notes |
|-----------|--------|-------|
| Lock Key-Value | ✅ | `PUT /locks/{key}` — sets `locked: true` |
| Unlock Key-Value | ✅ | `DELETE /locks/{key}` — sets `locked: false` |

### Revisions

> [REST reference](https://learn.microsoft.com/en-us/azure/azure-app-configuration/rest-api-revisions)

| Operation | Status | Notes |
|-----------|--------|-------|
| List Revisions | ✅ | `GET /revisions` |

### Labels

> [REST reference](https://learn.microsoft.com/en-us/azure/azure-app-configuration/rest-api-labels)

| Operation | Status | Notes |
|-----------|--------|-------|
| List Labels | ✅ | `GET /labels` |

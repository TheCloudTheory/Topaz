---
sidebar_position: 7
---

# Managed Identity

:::info[Azure REST API reference]
[Azure Managed Identity REST API · 2023-01-31](https://learn.microsoft.com/en-us/rest/api/managedidentity/user-assigned-identities?view=rest-managedidentity-2023-01-31)
:::

This page tracks which Azure Managed Identity REST API operations are implemented in Topaz. Managed Identity is control-plane only — there is no separate data plane.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

### User Assigned Identities

> [REST reference](https://learn.microsoft.com/en-us/rest/api/managedidentity/user-assigned-identities?view=rest-managedidentity-2023-01-31)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Delete | ✅ | |
| Get | ✅ | |
| List By Resource Group | ✅ | |
| List By Subscription | ✅ | |
| Update | ✅ | PATCH |

### Federated Identity Credentials

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |

### System Assigned Identities

| Operation | Status |
|-----------|--------|
| Get By Resource | ❌ |

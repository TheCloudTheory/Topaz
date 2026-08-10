---
sidebar_position: 9
---

# RBAC (Authorization)

:::info[Azure REST API reference]
[Role Assignments · 2022-04-01](https://learn.microsoft.com/en-us/rest/api/authorization/role-assignments?view=rest-authorization-2022-04-01) · [Role Definitions · 2022-04-01](https://learn.microsoft.com/en-us/rest/api/authorization/role-definitions?view=rest-authorization-2022-04-01)
:::

This page tracks which Azure RBAC / Authorization REST API operations are implemented in Topaz. Authorization is control-plane only — there is no separate data plane.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

### Role Assignments

> [REST reference](https://learn.microsoft.com/en-us/rest/api/authorization/role-assignments?view=rest-authorization-2022-04-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | Scope-based PUT |
| Delete | ✅ | Scope-based DELETE |
| Get | ✅ | Scope-based GET |
| List For Scope | ✅ | GET list at any ARM scope |
| Create By Id | ❌ | |
| Delete By Id | ❌ | |
| Get By Id | ❌ | |
| List For Resource | ❌ | |
| List For Resource Group | ❌ | |
| List For Subscription | ❌ | |

### Role Definitions

> [REST reference](https://learn.microsoft.com/en-us/rest/api/authorization/role-definitions?view=rest-authorization-2022-04-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Delete | ✅ | |
| Get | ✅ | |
| List | ✅ | |
| Get By Id | ✅ | Tenant-level `/providers/Microsoft.Authorization/roleDefinitions/{id}` |

### Permissions

| Operation | Status |
|-----------|--------|
| List For Resource | ❌ |
| List For Resource Group | ❌ |

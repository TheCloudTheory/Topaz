---
sidebar_position: 8
---

# Azure Resource Manager

:::info[Azure REST API reference]
[Deployments · 2021-04-01](https://learn.microsoft.com/en-us/rest/api/resources/deployments?view=rest-resources-2021-04-01) · [Resource Groups · 2021-04-01](https://learn.microsoft.com/en-us/rest/api/resources/resource-groups?view=rest-resources-2021-04-01) · [Subscriptions · 2021-01-01](https://learn.microsoft.com/en-us/rest/api/resources/subscriptions?view=rest-resources-2021-01-01)
:::

This page tracks which Azure Resource Manager REST API operations are implemented in Topaz.

## Legend

| Symbol | Meaning |
|--------|--------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

### Deployments

> [REST reference](https://learn.microsoft.com/en-us/rest/api/resources/deployments?view=rest-resources-2021-04-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | ARM template deployments |
| Delete | ✅ | |
| Get | ✅ | |
| List At Resource Group Scope | ✅ | |
| Validate | ✅ | POST validate |
| Cancel | ❌ | |
| Export Template | ❌ | |
| What If | ❌ | |
| List At Subscription Scope | ❌ | |
| List At Management Group Scope | ❌ | |
| List At Tenant Scope | ❌ | |

### Providers

| Operation | Status | Notes |
|-----------|--------|-------|
| Get | ✅ | Provider namespace lookup |
| List | ❌ | |
| Register | ❌ | |
| Unregister | ❌ | |

### Resource Groups

> [REST reference](https://learn.microsoft.com/en-us/rest/api/resources/resource-groups?view=rest-resources-2021-04-01)

| Operation | Status |
|-----------|--------|
| Create Or Update | ✅ |
| Delete | ✅ |
| Get | ✅ |
| List | ✅ |
| Check Existence | ❌ |
| Export Template | ❌ |
| Update | ❌ |

### Subscriptions

> [REST reference](https://learn.microsoft.com/en-us/rest/api/resources/subscriptions?view=rest-resources-2021-01-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | |
| Get | ✅ | |
| List | ✅ | |
| Update | ✅ | |
| Create Or Update Predefined Tags | ✅ | |

### Tags

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| List | ❌ |

### Resources

| Operation | Status |
|-----------|--------|
| Get | ❌ |
| List By Resource Group | ❌ |
| List | ❌ |
| Move Resources | ❌ |

---
sidebar_position: 8
---

# Azure Resource Manager

:::info[Azure REST API reference]
[Deployments · 2021-04-01](https://learn.microsoft.com/en-us/rest/api/resources/deployments?view=rest-resources-2021-04-01) · [Resource Groups · 2021-04-01](https://learn.microsoft.com/en-us/rest/api/resources/resource-groups?view=rest-resources-2021-04-01) · [Subscriptions (resources) · 2021-01-01](https://learn.microsoft.com/en-us/rest/api/resources/subscriptions?view=rest-resources-2021-01-01) · [Subscription API · 2021-10-01](https://learn.microsoft.com/en-us/rest/api/subscription/operation-groups?view=rest-subscription-2021-10-01)
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

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | The update path (PUT on existing group) is a no-op — tag and location changes are silently ignored |
| Delete | ✅ | Returns HTTP 200; Azure spec requires 202 Accepted |
| Get | ✅ | |
| List | ✅ | |
| Check Existence | ✅ | HEAD verb; returns 204 if exists, 404 if not |
| Export Template | ✅ | POST exportTemplate; supports IncludeParameterDefaultValue, IncludeComments, SkipResourceNameParameterization, SkipAllParameterization options |
| Update | ✅ | PATCH verb; updates tags |

### Subscriptions

> [REST reference — resources/subscriptions](https://learn.microsoft.com/en-us/rest/api/resources/subscriptions?view=rest-resources-2021-01-01) · [REST reference — Subscription API](https://learn.microsoft.com/en-us/rest/api/subscription/subscription?view=rest-subscription-2021-10-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | Topaz-specific; not a standard ARM operation in Azure |
| Get | ✅ | |
| List | ✅ | |
| Rename | ✅ | POST providers/Microsoft.Subscription/rename |
| List Locations | ✅ | Returns a static list of Azure regions |
| Cancel | ✅ | POST providers/Microsoft.Subscription/cancel; sets subscription state to Disabled |
| Enable | ✅ | POST providers/Microsoft.Subscription/enable; sets subscription state to Enabled |
| Create Or Update Predefined Tags | ✅ | PUT tagNames/&#123;tagName&#125;/tagValues/&#123;tagValue&#125; |
| Accept Ownership | ❌ | |
| Accept Ownership Status | ❌ | |

### Alias

> [REST reference](https://learn.microsoft.com/en-us/rest/api/subscription/alias?view=rest-subscription-2021-10-01)

| Operation | Status |
|-----------|--------|
| Create | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |

### Billing Account

> [REST reference](https://learn.microsoft.com/en-us/rest/api/subscription/billing-account?view=rest-subscription-2021-10-01)

| Operation | Status |
|-----------|--------|
| Get Policy | ❌ |

### Subscription Policy

> [REST reference](https://learn.microsoft.com/en-us/rest/api/subscription/subscription-policy?view=rest-subscription-2021-10-01)

| Operation | Status |
|-----------|--------|
| Add Update Policy For Tenant | ❌ |
| Get Policy For Tenant | ❌ |
| List Policy For Tenant | ❌ |

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

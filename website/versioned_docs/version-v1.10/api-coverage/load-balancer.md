---
sidebar_position: 14
---

# Load Balancer

:::info[Azure REST API reference]
[Azure Load Balancer REST API · 2024-03-01](https://learn.microsoft.com/en-us/rest/api/load-balancer/load-balancers?view=rest-load-balancer-2024-03-01)
:::

This page tracks which Azure Load Balancer REST API operations are implemented in Topaz.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

### Load Balancers

> [REST reference](https://learn.microsoft.com/en-us/rest/api/load-balancer/load-balancers?view=rest-load-balancer-2024-03-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `PUT /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}` |
| Get | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}` |
| Delete | ✅ | `DELETE /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}` |
| Update Tags | ✅ | `PATCH /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers/{name}` |
| List By Resource Group | ✅ | `GET /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Network/loadBalancers` |
| List By Subscription | ✅ | `GET /subscriptions/{sub}/providers/Microsoft.Network/loadBalancers` |
| List All | ❌ | |
| Update | ❌ | |

### Backend Address Pools

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Get | ❌ |
| Delete | ❌ |
| List | ❌ |

### Frontend IP Configurations

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Get | ❌ |
| Delete | ❌ |
| List | ❌ |

### Health Probes

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Get | ❌ |
| Delete | ❌ |
| List | ❌ |

### Load Balancing Rules

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Get | ❌ |
| Delete | ❌ |
| List | ❌ |

### Inbound NAT Rules

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Get | ❌ |
| Delete | ❌ |
| List | ❌ |

### Outbound Rules

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Get | ❌ |
| Delete | ❌ |
| List | ❌ |

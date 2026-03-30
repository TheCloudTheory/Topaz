---
sidebar_position: 6
---

# Virtual Network

:::info[Azure REST API reference]
[Azure Virtual Networks REST API](https://learn.microsoft.com/en-us/rest/api/virtualnetwork/virtual-networks?view=rest-virtualnetwork-2023-09-01)
:::

This page tracks which Azure Virtual Network REST API operations are implemented in Topaz. Virtual Network has no data plane — all operations are ARM control plane only.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

### Virtual Networks

> [REST reference](https://learn.microsoft.com/en-us/rest/api/virtualnetwork/virtual-networks?view=rest-virtualnetwork-2023-09-01)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Get | ✅ | |
| Delete | ❌ | |
| List | ❌ | Subscription-level listing not implemented |
| List All | ❌ | |
| Update Tags | ❌ | |
| Check IP Address Availability | ❌ | |
| List Ddos Protection Status | ❌ | |
| List Usage | ❌ | |

### Subnets

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| Prepare Network Policies | ❌ |
| Unprepare Network Policies | ❌ |

### Network Security Groups

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| List All | ❌ |
| Update Tags | ❌ |

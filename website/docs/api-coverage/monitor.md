---
sidebar_position: 10
---

# Monitor (Application Insights)

:::info[Azure REST API reference]
[Application Insights Components · 2020-02-02](https://learn.microsoft.com/en-us/rest/api/application-insights/components?view=rest-application-insights-2020-02-02) · [Activity Log Events](https://learn.microsoft.com/en-us/rest/api/monitor/activity-log-events)
:::

This page tracks which Azure Monitor and Application Insights REST API operations are implemented in Topaz.

## Legend

| Symbol | Meaning |
|--------|--------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane

### Application Insights Components

> [REST reference](https://learn.microsoft.com/en-us/rest/api/application-insights/components?view=rest-application-insights-2020-02-02)

| Operation | Status |
|-----------|--------|
| Create Or Update | ❌ |
| Delete | ❌ |
| Get | ❌ |
| List | ❌ |
| List By Resource Group | ❌ |
| Update Tags | ❌ |
| Purge | ❌ |
| Get Purge Status | ❌ |

---

## Data Plane

### Activity Log Events

> [REST reference](https://learn.microsoft.com/en-us/rest/api/monitor/activity-log-events)

| Operation | Status | Notes |
|-----------|--------|-------|
| List management events | ✅ | Returns empty array |

### Application Insights Data API

| Operation | Status |
|-----------|--------|
| Query (Analytics) | ❌ |
| Metrics | ❌ |
| Events | ❌ |

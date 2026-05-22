---
sidebar_position: 13
---

# App Service

:::info[Azure REST API reference]
- Control plane – Plans: [App Service Plans REST API · 2024-04-01](https://learn.microsoft.com/en-us/rest/api/appservice/app-service-plans)
- Control plane – Sites: [Web Apps REST API · 2024-04-01](https://learn.microsoft.com/en-us/rest/api/appservice/web-apps)
:::

This page tracks which Azure App Service REST API operations are implemented in Topaz.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Control Plane — App Service Plans (`Microsoft.Web/serverfarms`)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | |
| Get | ✅ | |
| Delete | ✅ | |
| List By Resource Group | ✅ | |
| List | ✅ | List all plans in subscription |
| Restart Web Apps | ✅ | Returns 200; no actual restart logic |

---

## Provider-level Operations (`Microsoft.Web`)

| Operation | Status | Notes |
|-----------|--------|-------|
| Check Name Availability | ✅ | `POST /subscriptions/{subscriptionId}/providers/Microsoft.Web/checknameavailability`; always returns available |

---

## Control Plane — Web Apps / Function Apps (`Microsoft.Web/sites`)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create Or Update | ✅ | `kind` field: `app`, `functionapp`, `functionapp,linux` |
| Get | ✅ | |
| Delete | ✅ | |
| List By Resource Group | ✅ | |
| List | ✅ | List all sites in subscription |

---

## Control Plane — Site Config sub-resource (`Microsoft.Web/sites/config`)

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Configuration | ❌ | `GET .../config/web` |
| Update Configuration | ❌ | `PUT .../config/web` |
| Update Application Settings | ❌ | `PUT .../config/appsettings` |
| List Application Settings | ❌ | `POST .../config/appsettings/list` |

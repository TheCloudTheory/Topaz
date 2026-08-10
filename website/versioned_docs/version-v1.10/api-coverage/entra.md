---
sidebar_position: 18
---

# Entra ID (Microsoft Graph)

:::info[Azure REST API reference]
[Microsoft Graph REST API](https://learn.microsoft.com/en-us/graph/api/overview)
:::

This page tracks which Microsoft Entra ID (Azure AD) operations are implemented in Topaz via the Microsoft Graph-compatible endpoint.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Implemented |
| ❌ | Not implemented |

---

## Authentication

| Operation | Status | Notes |
|-----------|--------|-------|
| Token (OAuth2) | ✅ | `POST /{tenantId}/oauth2/v2.0/token` — client credentials and device code grant types |
| Authorize | ✅ | `GET /{tenantId}/oauth2/v2.0/authorize` |
| Device Code | ✅ | `POST /{tenantId}/oauth2/v2.0/devicecode` |
| Device Login | ✅ | Interactive device login page |
| OIDC Discovery | ✅ | `GET /{tenantId}/.well-known/openid-configuration` |

---

## Users

> [REST reference](https://learn.microsoft.com/en-us/graph/api/resources/user)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | `POST /v1.0/users` |
| Get | ✅ | `GET /v1.0/users/{id}` |
| List | ✅ | `GET /v1.0/users` |
| Update | ✅ | `PATCH /v1.0/users/{id}` |
| Delete | ✅ | `DELETE /v1.0/users/{id}` |
| Me | ✅ | `GET /v1.0/me` |

---

## Groups

> [REST reference](https://learn.microsoft.com/en-us/graph/api/resources/group)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | `POST /v1.0/groups` |
| Get | ✅ | `GET /v1.0/groups/{id}` |
| List | ✅ | `GET /v1.0/groups` |
| Update | ✅ | `PATCH /v1.0/groups/{id}` |
| Delete | ✅ | `DELETE /v1.0/groups/{id}` |
| List Members | ✅ | `GET /v1.0/groups/{id}/members` |
| List Member Of | ✅ | `GET /v1.0/groups/{id}/memberOf` |
| List Owners | ✅ | `GET /v1.0/groups/{id}/owners` |

---

## Applications

> [REST reference](https://learn.microsoft.com/en-us/graph/api/resources/application)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | `POST /v1.0/applications` |
| Get | ✅ | `GET /v1.0/applications/{id}` |
| List | ✅ | `GET /v1.0/applications` |
| Update | ✅ | `PATCH /v1.0/applications/{id}` |
| Delete | ✅ | `DELETE /v1.0/applications/{id}` |
| Add Password | ✅ | `POST /v1.0/applications/{id}/addPassword` |
| List Owners | ✅ | `GET /v1.0/applications/{id}/owners` |
| Remove Owner | ✅ | `DELETE /v1.0/applications/{id}/owners/{ownerId}/$ref` |

---

## Service Principals

> [REST reference](https://learn.microsoft.com/en-us/graph/api/resources/serviceprincipal)

| Operation | Status | Notes |
|-----------|--------|-------|
| Create | ✅ | `POST /v1.0/servicePrincipals` |
| Get | ✅ | `GET /v1.0/servicePrincipals/{id}` |
| List | ✅ | `GET /v1.0/servicePrincipals` |
| Update | ✅ | `PATCH /v1.0/servicePrincipals/{id}` |
| Delete | ✅ | `DELETE /v1.0/servicePrincipals/{id}` |
| List Owners | ✅ | `GET /v1.0/servicePrincipals/{id}/owners` |
| Remove Owner | ✅ | `DELETE /v1.0/servicePrincipals/{id}/owners/{ownerId}/$ref` |

---

## Directory

| Operation | Status | Notes |
|-----------|--------|-------|
| Get Directory | ✅ | `GET /v1.0/directory` |

---

## Tenant Relationships

| Operation | Status | Notes |
|-----------|--------|-------|
| Find Tenant Information By Tenant Id | ✅ | `GET /v1.0/tenantRelationships/findTenantInformationByTenantId` |

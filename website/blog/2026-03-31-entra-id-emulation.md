---
slug: entra-id-emulation
title: How Topaz emulates Microsoft Entra ID
authors: kamilmrzyglod
tags: [entra]
---

Authentication is the first thing every Azure SDK touches. Before your application can read a secret from Key Vault, publish a message to Service Bus, or query a storage account, it needs a valid token. That token comes from Microsoft Entra ID. Without a working Entra emulation layer, every other Azure service emulator is incomplete — your code would still call out to `login.microsoftonline.com` even in a fully local setup.

Topaz solves this by shipping a full Entra ID emulation layer out of the box.

<!-- truncate -->

## A pre-configured local tenant

When Topaz starts, it automatically provisions a local Entra tenant called `topaz.local.dev` with a tenant ID of `50717675-3E5E-4A1E-8CB5-C62D8BE8CA48`. There is nothing to configure — the tenant, its OIDC discovery document, and a built-in superadmin user (`topazadmin@topaz.local.dev` / `admin`) are all ready the moment the host is running. See the [getting started guide](/docs/intro) for installation and one-time DNS / certificate setup.

All authentication requests that SDKs normally send to `https://login.microsoftonline.com` are intercepted by Topaz and handled locally. Your application code does not need to change.

## Real JWTs, real validation

Topaz does not return dummy tokens or bypass token validation. Every access token it issues is a properly structured, signed JWT — the same format Azure issues in production. Tokens are signed with a symmetric key (HMAC-SHA256), have a one-hour lifetime, and carry the standard claims (`sub`, `iss`, `aud`, `iat`, `exp`) alongside an `oid` identifying the principal.

Because the tokens are real JWTs, any code in your application that decodes or validates a token will behave exactly as it would against Azure.

An `id_token` is also returned in flows that request it. It follows the OIDC spec: unsigned (`alg: none`), with claims that reflect the authenticated user's profile and tenant.

## All common grant types are supported

The token endpoint at `/organizations/oauth2/v2.0/token` handles the four grant types you are most likely to use:

| Grant type | Typical use case |
|---|---|
| `client_credentials` | Service-to-service authentication (daemon apps, background workers) |
| `password` | Username / password sign-in in headless or test scenarios |
| `authorization_code` | Browser-based interactive login flows |
| `refresh_token` | Renewing an existing session without re-authenticating |

The `client_credentials` flow is the one Azure services use internally — it validates the `client_secret` against the application registration stored in Topaz, so the full credential lifecycle is exercised rather than short-circuited.

## A local Microsoft Graph API

Entra ID is more than just tokens. Applications routinely call the Microsoft Graph API to manage users, register applications, and look up service principals. Topaz implements a subset of the Graph v1.0 API covering the resources you actually need during development:

- **Users** — create, list, get, and delete users; all fields including `userPrincipalName`, `displayName`, and password profile are supported.
- **Applications** — full CRUD including generating and revoking client secrets via `/applications/{id}/addPassword`.
- **Service Principals** — create, update, and delete service principal objects linked to application registrations.
- **Groups** — listing groups is supported for compatibility with SDKs that enumerate group memberships.

Every resource you create is persisted to disk under `.topaz/.entra/`, so users and application registrations survive Topaz restarts exactly as they would in a real tenant.

## OIDC discovery

The OIDC discovery endpoint at `/.well-known/openid-configuration` is fully functional. SDKs that follow the standard auto-discovery flow — fetching the configuration document before acquiring a token — work without any special handling. Both the `/organizations/` and `/{tenantId}/` variants are served.

## No code changes required

The goal of Topaz's Entra emulation is zero friction. If your application is already using `DefaultAzureCredential`, `ClientSecretCredential`, or the Microsoft Authentication Library (MSAL), it will authenticate against Topaz without modification as long as the authority URL is pointed at the local host. For application registrations created through the Azure portal you can mirror them in Topaz using the Graph API or the Topaz portal, then supply the same `client_id` and `client_secret` in your local configuration.

For step-by-step integration guides, see:
- [ASP.NET Core integration](/docs/ecosystem/aspnet-core) — provision local infrastructure at application startup using `AddTopaz()`.
- [Azure CLI integration](/docs/azure-cli-integration) — register the local cloud environment and run `az` commands against Topaz.
- [Troubleshooting authentication errors](/docs/troubleshooting#authenticationfailedexception--401-responses) — common causes and fixes for `AuthenticationFailedException` and `CredentialUnavailableException`.

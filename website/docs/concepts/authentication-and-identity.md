---
sidebar_position: 4
description: How Topaz emulates Entra ID (Azure Active Directory) — token issuance, credential flows, and what the local identity layer does and does not replicate.
keywords: [topaz authentication, topaz entra id, azure active directory local, local azure authentication, topaz tokens, azure credential local emulator]
---

# Authentication and identity

Topaz emulates the Entra ID (Azure Active Directory) authentication layer that Azure SDKs, the Azure CLI, and Terraform providers depend on. Understanding what Topaz's identity layer does — and what it deliberately omits — helps when debugging authentication failures and when deciding whether a particular auth flow is supported locally.

## Why authentication matters for local emulation

Every Azure SDK call, every `az` command, and every Terraform provider operation includes authentication. The Azure SDK's `DefaultAzureCredential` acquires a token from Entra ID before making any API call. Without a working identity layer, the entire emulation falls apart — even if the service data planes are implemented correctly.

Topaz ships its own Entra endpoint at `https://topaz.local.dev:8899`. When clients perform endpoint discovery (by querying the ARM metadata endpoint), they are redirected to Topaz's Entra implementation instead of `login.microsoftonline.com`.

## Supported credential flows

Topaz supports the credential acquisition flows that are most common in local development:

| Flow | How to use |
|---|---|
| **Azure CLI credential** | `az login` against Topaz, then `DefaultAzureCredential` picks it up automatically |
| **Device code flow** | `az login` opens a device code prompt; Topaz handles the authentication locally |
| **ROPC (username/password)** | `az login --username --password`; requires `HTTPS_PROXY=http://127.0.0.1:44380` to route through Topaz's proxy |
| **Client credentials (service principal)** | Supported; the client ID and secret are validated against Topaz's Entra emulation |
| **Managed Identity** | Topaz issues MSI tokens for resources that support Managed Identity |

The standard pattern for local development is `az login` (device code), followed by any SDK or CLI operation that uses `DefaultAzureCredential`. The credential resolves through the Azure CLI token cache, which contains a token issued by Topaz's Entra layer.

## Tokens issued by Topaz

Topaz issues JWTs that are structurally valid and contain the expected claims (tenant ID, object ID, audience, expiry). These tokens are verified by Topaz's own service implementations — Key Vault, Service Bus, Storage — to enforce that calls are authenticated.

Because these tokens are issued by Topaz's private key, they are not valid against real Azure. A token acquired from Topaz cannot be used to call `vault.azure.net`. This is by design: local tokens stay local.

## Entra resource management

Topaz emulates the Entra resource model used by the `azuread` Terraform provider and the Microsoft Graph-adjacent operations that applications rely on:

- Applications and service principals
- Users and groups
- Directory roles

This is the layer targeted by `terraform apply` when using the `azuread` provider. Resources created here persist in Topaz's local state store and can be referenced by other resources within the same Topaz instance.

## What is not emulated

Topaz does not attempt to replicate every aspect of real Entra ID. Notably absent:

- **Conditional Access policies** — all authenticated requests are accepted if the token is structurally valid
- **MFA and SSPR flows** — Topaz issues tokens without challenging for a second factor
- **Real tenant isolation** — there is no boundary between tenants in a Topaz instance
- **Microsoft Graph API** — Graph endpoints are not implemented; only the Entra primitives needed for resource provisioning are available
- **External identity providers and federation** — B2C and federated identity scenarios are out of scope

If your application logic depends on Conditional Access decisions, MFA prompts, or Graph API calls, those code paths will not behave the same way locally. Plan for this when deciding which scenarios to cover in local tests.

## The tenant ID

Topaz uses a fixed local tenant ID for all token issuance. When you run `az login` against Topaz, the resulting token's `tid` claim is set to this local tenant. SDKs that validate the tenant ID against a known value will need to be configured to accept the Topaz tenant ID in local environments.

See the [Getting started](../intro.md) guide for the exact `az login` commands and the fixed tenant ID value in use.

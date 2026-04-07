---
slug: acr-data-plane
title: How Topaz emulates the Azure Container Registry data plane
authors: kamilmrzyglod
tags: [general]
---

Container Registry is different from every other Azure service Topaz emulates. You do not call it through the Azure SDK with a credential — you call it through the Docker CLI, `docker pull`, `docker push`, `helm push`, or any OCI-compliant client. Before any of that works, the client has to authenticate. And that authentication flow is entirely its own protocol, separate from anything in the Azure REST API.

This post walks through how Topaz emulates the ACR data plane authentication layer and what design decisions made it possible without writing a custom token server.

<!-- truncate -->

## The Docker Registry V2 authentication protocol

When `docker pull` contacts a registry for the first time, it starts with a simple probe: `GET /v2/`. If the registry requires authentication — and ACR always does — it responds with a `401 Unauthorized` and a `Www-Authenticate` header that tells the client exactly where to go next:

```
HTTP/1.1 401 Unauthorized
Www-Authenticate: Bearer realm="https://myregistry.azurecr.io/oauth2/token",service="myregistry.azurecr.io"
```

The client then goes to that `realm` URL, obtains a token, and retries `GET /v2/` with a `Bearer` header. Azure Container Registry adds its own twist to this flow: before the client can get a registry access token, it first has to exchange its Entra ID access token for an ACR-specific **refresh token** at `/oauth2/exchange`. `az acr login` handles this transparently, which is why most developers never see it.

The full flow looks like this:

```
1. GET /v2/                        → 401 + Www-Authenticate challenge
2. POST /oauth2/exchange            → exchange AAD token for ACR refresh token
3. GET /oauth2/token                → exchange refresh token for short-lived access token  
4. GET /v2/ (with Bearer token)     → 200 OK — client is authenticated
```

Topaz implements steps 1 and 2. Step 3 reuses the same token from step 2 (the refresh token is also valid as an access token in a local context), and step 4 validates it against the same JWT infrastructure that every other Topaz service uses.

## Per-registry hostname routing

Each registry in ACR has its own login server hostname: `myregistry.azurecr.io`. Docker clients embed this hostname in every request — the `Host` header is the only way to tell which registry is being addressed when requests arrive at the same IP.

Topaz follows the same model. When you create a Container Registry resource, Topaz sets its `loginServer` property to a subdomain under `cr.topaz.local.dev`:

```
myregistry.cr.topaz.local.dev:8892
```

All registry hostnames resolve to the same Topaz host (via the local DNS wildcard entry). When a request arrives at port 8892, Topaz reads the `Host` header, strips the subdomain prefix as the registry name, and looks it up in its internal DNS table to resolve the subscription and resource group context. From there it can load the corresponding registry resource, check its properties (such as whether admin access is enabled), and respond accordingly.

```csharp
private ContainerRegistryResource? ResolveRegistry(HttpContext context)
{
    var hostName = context.Request.Host.Host; // e.g. "myregistry.cr.topaz.local.dev"
    var registryName = hostName.Split('.')[0];

    var identifiers = GlobalDnsEntries.GetEntry(ContainerRegistryService.UniqueName, registryName);
    if (identifiers == null) return null;

    var operation = controlPlane.Get(
        SubscriptionIdentifier.From(identifiers.Value.subscription),
        ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!),
        registryName);

    return operation.Resource;
}
```

No routing table to configure. No per-registry port assignments. The subdomain _is_ the routing key.

## The challenge endpoint

`GET /v2/` is the entrypoint for every Docker client interaction. Topaz handles three cases:

**No `Authorization` header** — the client has not authenticated yet. Topaz returns a `401` with a `Www-Authenticate` challenge pointing at `/oauth2/token` on the same host:

```
Www-Authenticate: Bearer realm="https://myregistry.cr.topaz.local.dev:8892/oauth2/token",
                         service="myregistry.cr.topaz.local.dev:8892"
```

**`Basic` credentials** — used by tools that call `docker login` with a username and password rather than going through the Entra token exchange. This is the admin user flow. Topaz looks up the registry, checks whether `adminUserEnabled` is true, and validates the supplied username and password against the stored admin credentials. If any check fails, a challenge is returned instead of a `200`. If admin access is disabled on the registry, Basic auth is rejected outright — matching ACR's own behaviour.

**`Bearer` token** — the client has already completed the exchange flow. Topaz validates the JWT using the same symmetric key used for every other token it issues. A valid token returns `200 {}`. An invalid or expired one returns another challenge.

## Exchanging an Entra token for an ACR token

`POST /oauth2/exchange` is where `az acr login` sends the AAD access token it obtained from Entra. The request body is a form-encoded payload with `grant_type=access_token_refresh_token`, the `service` name, and the `access_token`.

Topaz reads the `access_token` from the form body, validates it as a Topaz JWT, and extracts the `sub` claim to identify the caller. It then mints a new token scoped to the same subject and returns it as the `refresh_token` in the response:

```csharp
var validated = JwtHelper.ValidateJwt(aadToken);
var objectId = validated?.Subject ?? Globals.GlobalAdminId;

var refreshToken = JwtHelper.IssueAcrToken(objectId);

var payload = JsonSerializer.Serialize(
    new { refresh_token = refreshToken },
    GlobalSettings.JsonOptions);
```

If the incoming token cannot be validated (expired, tampered, or absent), Topaz falls back to the global admin identity rather than rejecting the request. This is intentional: in a local development context, an Entra token may have been issued by a real Azure tenant and will obviously fail HMAC validation against the local key. Falling back to the admin identity means `az acr login` succeeds without requiring a perfectly routed token. On a connected machine where the real Entra token flows through Topaz's own emulated Entra layer, the `sub` claim is preserved end-to-end.

## Reusing the JWT infrastructure

One of the core decisions in Topaz's design is that every identity primitive — Entra access tokens, Graph tokens, Managed Identity tokens, and now ACR tokens — uses the same signing key and the same `JwtHelper`. There is no separate token server for the registry. The token the exchange endpoint returns is a standard Topaz JWT with the same claims layout and one-hour expiry as everything else.

This means:

- Validation is consistent: `JwtHelper.ValidateJwt` works for every token type.
- There are no new secrets to manage or rotate in a local environment.
- Any tooling that already trusts the Topaz TLS certificate and DNS entries gets ACR authentication for free.

## Admin credentials

When you create a Container Registry with `adminUserEnabled: true`, Topaz generates an admin username (same as the registry name) and a random password. These are stored on disk as part of the registry resource and returned by the `POST /registries/{name}/listCredentials` endpoint. Tools like `docker login` that prefer username/password over AAD can use these directly.

Updating a registry to disable admin access clears the stored credentials. Re-enabling admin access generates a fresh password, matching ACR's credential rotation behaviour when toggling the admin user.

## What is not yet implemented

The authentication layer described here is the foundation, not the ceiling. The OCI Distribution Spec defines a broader set of endpoints for manifests, blobs, and tags — the actual content of the registry. Full OCI data plane support (blob uploads, manifest push, blob existence checks) has since landed in Topaz. See the follow-up post [Pushing images to Topaz: how the OCI data plane works](/blog/acr-oci-data-plane) for the full picture.

`docker pull` and tag listing remain on the roadmap. For now, the control plane (creating, updating, and deleting registries via ARM), the authentication layer, and the write path (`docker push`) are stable and ready to use.

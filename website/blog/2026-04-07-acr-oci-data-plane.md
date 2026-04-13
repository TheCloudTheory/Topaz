---
slug: acr-oci-data-plane
title: "Pushing images to Topaz: how the OCI data plane works"
authors: kamilmrzyglod
tags: [general]
---

The [previous post on ACR authentication](/blog/acr-data-plane) covered everything up to the point where `docker login` succeeds and Docker has a valid Bearer token. That is the precondition. The question this post answers is what happens next: how `docker push` transfers a real image into Topaz and why the protocol is more structured than a simple file upload.

{/* truncate */}

## What Docker actually sends

A container image is not a single file. It is a set of layer tarballs — one per filesystem layer — plus a configuration object (a JSON document describing the image's environment, entrypoint, and other metadata), and a manifest that ties them together by digest. `docker push` sends each of these as a separate upload, in a specific order, before it sends the manifest that references them.

The OCI Distribution Specification (and the Docker Registry HTTP API v2 it is based on) defines a three-phase upload protocol for each blob:

```
1. POST /v2/{repository}/blobs/uploads/
   → 202 Accepted, Location: .../blobs/uploads/{uuid}

2. PATCH /v2/{repository}/blobs/uploads/{uuid}   (one or more times)
   → 202 Accepted, Range: 0-{n}

3. PUT /v2/{repository}/blobs/uploads/{uuid}?digest=sha256:{hex}
   → 201 Created, Docker-Content-Digest: sha256:{hex}
```

After every blob is stored, Docker verifies each one with a `HEAD /v2/{repository}/blobs/{digest}` request before it pushes the manifest. Only once all blobs are confirmed does it send the final `PUT /v2/{repository}/manifests/{reference}`.

Topaz now implements all of these endpoints.

## Session-based uploads

The three-phase protocol exists because blobs can be large and networks are unreliable. Opening a session with a `POST` gives the client a `uuid` it can use to resume an interrupted upload. Topaz models this directly using the filesystem:

- **Initiate** (`POST /v2/{repo}/blobs/uploads/`): Topaz creates an empty file under `uploads/{uuid}` inside the registry's data directory and returns the UUID. Nothing is written to permanent storage yet.
- **Append** (`PATCH .../blobs/uploads/{uuid}`): Each `PATCH` request opens the upload file in append mode, copies the request body into it, and reports the byte range received so far (`0-{end}`) in the `Range` response header. Multiple `PATCH` calls accumulate bytes in the same file.
- **Complete** (`PUT .../blobs/uploads/{uuid}?digest=sha256:{hex}`): The final `PUT` may carry a last chunk in its body or may have an empty body if all data was sent via `PATCH`. Topaz appends any body content, computes the SHA-256 digest of the accumulated bytes, and compares it against the `digest` query parameter. On a match, the temporary file is moved to `blobs/sha256/{hex}` — the content-addressable store. On a mismatch, the session file is deleted and `400 DIGEST_INVALID` is returned.

The digest check is the key integrity control. Docker always computes the digest before pushing and sends it as a query parameter — Topaz recomputes it server-side and rejects mismatches. No corrupted or tampered blob can enter the store.

## Content-addressable storage

Every stored blob lives at a path derived entirely from its content:

```
.topaz/
  .resource-groups/{sub}/{rg}/.container-registry/{registry}/
    data/
      blobs/
        sha256/
          83b2d7e29698161422a104647ffb26568fe86648ff3609d1b60a4f9e9de38074
      uploads/
        {uuid}          ← in-progress sessions
      manifests/
        {repository}/
          v1.json       ← stored by tag
          83b2d7e2….json ← also stored by digest
```

Storing blobs by digest means deduplication is free. If two images share a layer, only one copy is written. It also means any attempt to request a blob is answered by a deterministic path lookup — no index, no database.

Topaz guards against path traversal at every step. All user-supplied values (registry name, UUID, digest hex) pass through `PathGuard.ValidateName` and `PathGuard.EnsureWithinDirectory` before being combined into filesystem paths. The digest hex is additionally validated to be exactly 64 lowercase hexadecimal characters — the canonical SHA-256 output length — before it is used as a filename.

## Verifying blob presence

Between completing an upload and pushing the manifest, Docker sends a `HEAD /v2/{repo}/blobs/{digest}` for every blob it expects to find. If any returns a `404`, the push aborts with `unknown blob`. Topaz handles this by looking up the blob in the content-addressable store and returning the file's byte count as `Content-Length` with a `200 OK` if found, or `404 BLOB_UNKNOWN` if not:

```csharp
var size = dataPlane.GetBlobLength(sub, rg, registryName, digest);

if (size == null)
{
    response.CreateJsonContentResponse(
        "{\"errors\":[{\"code\":\"BLOB_UNKNOWN\",\"message\":\"blob not found\"}]}",
        HttpStatusCode.NotFound);
    return;
}

response.Headers.Add("Docker-Content-Digest", digest);
response.Content = new ByteArrayContent([]);
response.Content.Headers.ContentLength = size.Value;
response.StatusCode = HttpStatusCode.OK;
```

`Content-Length` on a `HEAD` response is how Docker determines it has the complete blob rather than a partial one, so it must be accurate.

## Manifests

Once all blobs have been confirmed, Docker pushes the image manifest with `PUT /v2/{repo}/manifests/{reference}`, where `reference` is either a tag (`v1`, `latest`) or a digest (`sha256:…`). The manifest body is a JSON document that references each layer and the image config by digest.

Topaz stores a manifest in two ways simultaneously. First by tag, so `docker pull myregistry/myimage:v1` works. Second by the manifest's own content digest, so `docker pull myregistry/myimage@sha256:…` also works. Both forms are serialised inside a `ManifestEnvelope` that preserves the original `Content-Type` header alongside the raw bytes — necessary because OCI and Docker manifest types have different media types that clients use to interpret the document structure.

```csharp
var envelope = new ManifestEnvelope
{
    ContentType = contentType,
    Content     = manifestBytes,
    Digest      = ComputeDigest(manifestBytes)
};
```

The `Docker-Content-Digest` header in the `201 Created` response carries the manifest digest. This is what the Docker CLI prints as the image digest after a successful push.

## Routing data-plane requests

Every data-plane request arrives at port 8892 (the `ContainerRegistryPort`) rather than the ARM port 8899. The registry to target is determined by the `Host` header, not by any path prefix — exactly as in production ACR. Topaz reads the subdomain from `Host`, looks it up in `GlobalDnsEntries`, and resolves the subscription and resource group context from there. All data-plane endpoints share a `ResolveRegistry` helper that performs this lookup and returns a typed tuple or `null` on miss:

```csharp
private static (SubscriptionIdentifier, ResourceGroupIdentifier, string)?
    ResolveRegistry(HttpContext context)
{
    var registryName = context.Request.Host.Host.Split('.')[0];
    var identifiers = GlobalDnsEntries.GetEntry(
        ContainerRegistryService.UniqueName, registryName);
    if (identifiers == null) return null;
    return (SubscriptionIdentifier.From(identifiers.Value.subscription),
            ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!),
            registryName);
}
```

A `null` from `ResolveRegistry` maps to `404 NAME_UNKNOWN` — the error the OCI spec defines for a repository that does not exist on the server.

## A complete push, end to end

Running `docker push topazacrpush01.cr.topaz.local.dev:8892/topaz-test-image:v1` against a running Topaz now produces the following sequence of requests, all handled locally:

| Method | Path | Result |
|--------|------|--------|
| `GET` | `/v2/` | `401` — Bearer challenge |
| `POST` | `/oauth2/exchange` | `200` — ACR refresh token |
| `GET` | `/oauth2/token` | `200` — scoped access token |
| `GET` | `/v2/` | `200` — authenticated |
| `POST` | `/v2/topaz-test-image/blobs/uploads/` | `202` — upload session opened |
| `PATCH` | `/v2/topaz-test-image/blobs/uploads/{uuid}` | `202` — bytes received |
| `PUT` | `/v2/topaz-test-image/blobs/uploads/{uuid}?digest=…` | `201` — blob committed |
| `HEAD` | `/v2/topaz-test-image/blobs/{digest}` | `200` — blob confirmed |
| `PUT` | `/v2/topaz-test-image/manifests/v1` | `201` — push complete |

No traffic leaves the machine. No real ACR subscription is needed.

## What is not yet implemented

Topaz currently handles the write path: `docker push` works end-to-end. The read path (`docker pull`, `docker run`) requires `GET /v2/{repo}/blobs/{digest}` and `GET /v2/{repo}/manifests/{reference}`, which are on the roadmap. Tag listing (`GET /v2/{repo}/tags/list`) and repository enumeration (`GET /v2/_catalog`) are also planned.

For teams that only need to push images as part of a build pipeline — for example, to test that an ACR task or a deployment manifest references the correct digest — the current implementation is already useful.

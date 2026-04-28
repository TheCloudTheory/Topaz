---
sidebar_position: 9
description: Known limitations of Topaz — behaviours that differ from real Azure due to deliberate design trade-offs, planned for a future release.
keywords: [topaz limitations, azure emulator limitations, storage ports, topaz known issues]
---

# Known limitations

This page documents deliberate design trade-offs in the current version of Topaz that differ from real Azure behaviour. Each entry notes the impact and the milestone where the limitation is expected to be resolved.

## Azure Storage — per-service ports

**Affected services:** Blob Storage (8891), Table Storage (8890), Queue Storage (8893), File Storage (8894)

Real Azure exposes all storage data-plane services on a single HTTPS endpoint (`https://<account>.blob.core.windows.net`, `…table…`, etc., all on port 443). Topaz currently assigns a separate HTTP port to each sub-service.

### Impact

Some Azure SDK and CLI code paths construct a storage URL independently of any `--blob-endpoint` / `--connection-string` argument passed by the caller. Specifically, the Azure CLI's `get_content_setting_validator` (used by `az storage blob update` and similar commands) pre-fetches existing blob properties through `cf_blob_service`, which internally calls `get_account_url()` — building an `https://` URL from the cloud-suffix `storage_endpoint`. Because Topaz registers a single suffix (`storage.topaz.local.dev:8890`) that points to the Table Storage HTTP port, this path produces an SSL record-layer failure.

**Workaround:** pass a full `--connection-string` with an explicit `BlobEndpoint=http://…:8891` instead of separate `--account-name` / `--blob-endpoint` arguments for commands that trigger the validator (e.g. `az storage blob update`).

```bash
az storage blob update \
  --container-name mycontainer \
  --name myblob.txt \
  --content-type text/plain \
  --connection-string "AccountName=<name>;AccountKey=<key>;BlobEndpoint=http://<name>.blob.storage.topaz.local.dev:8891"
```

### Planned fix — v1.6-beta

Consolidate all storage data-plane services onto a single HTTPS port with subdomain-based routing in the Topaz router. This aligns the port topology with real Azure and removes the need for per-service port constants.

## Table Storage — explicit endpoint required in connection strings

**Affected services:** Table Storage (port 8890)

Table Storage data-plane endpoints are served over HTTPS (port 8890) using the Topaz self-signed certificate, which covers `*.table.storage.topaz.local.dev`. However, connection strings must always specify an explicit `TableEndpoint=https://…:8890` component. If you rely on `DefaultEndpointsProtocol=https` alone (without an explicit `TableEndpoint`), the Azure SDK derives the table URL from the default cloud suffix (`core.windows.net`), which does not resolve to the Topaz emulator.

### Impact

Constructing a `TableServiceClient` with only `DefaultEndpointsProtocol=https;AccountName=…;AccountKey=…` will attempt to reach `https://<account>.table.core.windows.net` rather than the Topaz host.

**Workaround:** always use `TopazResourceHelpers.GetAzureStorageConnectionString(...)`, which explicitly sets `TableEndpoint=https://…:8890`.

### Planned fix — v1.6-beta

Covered by the single-port HTTPS consolidation planned for Blob/Table/Queue/File storage (see above).

## Key Vault — AES symmetric key (`oct`) cryptographic operations not supported

**Affected services:** Key Vault data plane — `encrypt`, `decrypt`, `wrapKey`, `unwrapKey`

The current implementation of Key Vault crypto operations (`encrypt`, `decrypt`, `wrapKey`, `unwrapKey`) only supports **RSA key types** (`RSA`, `RSA-HSM`). AES symmetric algorithms — `A128GCM`, `A192GCM`, `A256GCM`, `A128CBC`, `A192CBC`, `A256CBC`, `A128CBCPAD`, `A192CBCPAD`, `A256CBCPAD` — require `oct` key material which is not yet stored or modelled in `KeyBundle`.

### Impact

Calling `az keyvault key encrypt --algorithm A256GCM` (or the SDK equivalent via `CryptographyClient.Encrypt(EncryptionAlgorithm.A256Gcm, ...)`) against any key in Topaz returns `400 BadParameter`. Creating an `oct` key via `POST /keys/{name}/create` with `"kty": "oct"` succeeds but the key object carries no usable material.

**Workaround:** use RSA key types (`RSA`, `RSA-HSM`) with algorithms `RSA1_5`, `RSA-OAEP`, or `RSA-OAEP-256` — these are fully supported.

### Planned fix — v1.4-beta

Extend `KeyBundle` / `JsonWebKey` with a `k` field to store raw symmetric key material, and implement AES-GCM and AES-CBC(PAD) encrypt/decrypt dispatch in the data plane.

## Key Vault — `Get Key Attestation` returns no attestation blob

**Affected services:** Key Vault data plane — `GET /keys/{name}/{version}/attestation`

The `Get Key Attestation` endpoint (`GET …/attestation`, API version 2025-07-01) is implemented and returns a valid `KeyBundle`. However, the `attributes.attestation` field — which on real hardware-backed (HSM) keys contains a PEM certificate chain and opaque attestation blobs proving the key never left the HSM boundary — is always `null` in Topaz.

This matches exactly how real Azure Key Vault behaves for **software-backed keys** (`kty=RSA`, `kty=EC` without the `-HSM` suffix): Azure also returns `null` for the `attestation` field on non-HSM keys. The difference is that real Azure HSM-backed key types (`RSA-HSM`, `EC-HSM`, Managed HSM) produce real attestation material; Topaz does not.

### Impact

Callers that create software-backed RSA or EC keys and call `GetKeyAttestation` receive the expected response — `key`, `attributes`, `tags` populated and `attributes.attestation = null` — which is identical to the real service response for the same key type. No code change is required for those scenarios.

Callers that specifically test HSM key attestation (i.e. rely on `attributes.attestation` being non-null) cannot be fully validated against Topaz.

**Workaround:** there is no workaround for HSM attestation testing within Topaz. Use a real Azure Managed HSM instance for any test logic that inspects `certificatePemFile`, `privateKeyAttestation`, or `publicKeyAttestation`.

### No fix planned

Replicating real HSM attestation behaviour requires generating hardware-backed key material and signing certificate chains with an HSM root, which is outside the scope of a software emulator. The `attestation` field will remain `null` for all Topaz-managed keys.

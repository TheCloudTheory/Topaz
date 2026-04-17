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

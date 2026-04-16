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

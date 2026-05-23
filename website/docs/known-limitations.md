---
sidebar_position: 9
description: Known limitations of Topaz — behaviours that differ from real Azure due to deliberate design trade-offs, planned for a future release.
keywords: [topaz limitations, azure emulator limitations, storage ports, topaz known issues]
---

# Known limitations

This page documents deliberate design trade-offs in the current version of Topaz that differ from real Azure behaviour. Each entry notes the impact and the milestone where the limitation is expected to be resolved.

## Key Vault — `WWW-Authenticate` challenge resource does not match emulator domain

**Affected services:** Key Vault data plane (secrets, keys, certificates)

When a Key Vault request is made without a bearer token, Topaz returns a `401 Unauthorized` response whose `WWW-Authenticate` header includes a `resource` claim of `https://vault.azure.net` — the same resource identifier used by real Azure. Some Azure SDK client libraries (including the Python `azure-keyvault-*` packages) verify that this resource matches the domain they used to reach the vault. Because the Topaz emulator is reached at a custom domain (e.g. `https://pytest-kv.vault.topaz.local.dev:8898`) the resource `vault.azure.net` does not match, and the SDK rejects the challenge with a `ValueError`.

### Impact

Python clients (and potentially other non-.NET SDK clients) that verify the challenge resource will raise an exception before issuing any authenticated Key Vault request. The `.NET` Azure SDK does not perform this verification and is unaffected.

**Workaround:** pass `verify_challenge_resource=False` to the Key Vault client constructor (Python SDK ≥ 4.11.0). Earlier SDK versions expose a module-level `verify_challenge_resource` function that can be replaced with a no-op before instantiating the client.

```python
from azure.keyvault.secrets import SecretClient

client = SecretClient(
    vault_url="https://my-vault.vault.topaz.local.dev:8898",
    credential=credential,
    verify_challenge_resource=False,   # required against Topaz
)
```

### Planned fix — v1.6-beta

Change the `WWW-Authenticate` header that Topaz emits to use the vault's actual request URL as the resource identifier instead of the hard-coded `vault.azure.net` value. This removes the domain mismatch and means clients that verify the challenge resource will work without any code change.

---

## AMQP — AMQPNetLite protocol deviations break non-.NET clients

**Affected services:** Service Bus, Event Hubs (AMQP data plane)

Topaz's AMQP server is built on [AMQPNetLite 2.5.1](https://github.com/Azure/amqpnetlite). While AMQPNetLite is fully compatible with .NET Azure SDK clients, it exhibits two behaviours that violate the AMQP 1.0 specification and break non-.NET clients:

1. **Trailing null fields are omitted.** AMQPNetLite encodes AMQP performatives (Attach, Transfer, Flow, Disposition, etc.) by writing only the fields up to the last non-null value and omitting any trailing optional fields. The AMQP 1.0 spec permits this as an optimisation, but several non-.NET AMQP clients (including the Python `pyamqp` used by `azure-servicebus`) access performative fields by **fixed numeric index** rather than by name, and raise `IndexError` when the encoded frame is shorter than expected.

2. **`Error` objects are encoded with two fields instead of three.** AMQPNetLite encodes an `Error` composite as `[condition, description]`, omitting the optional `info` (third) field. Clients that unconditionally access `error[2]` crash when receiving any Detach or Close frame that carries an error.

The Python `azure-servicebus` package currently requires monkey-patching in the test harness to work around both issues. Any other non-.NET AMQP client written against the spec will face the same problems.

### Impact

Non-.NET clients (Python, Go, JavaScript, etc.) cannot use the Service Bus or Event Hubs AMQP endpoints without client-side workarounds. The .NET Azure SDK is unaffected.

**Workaround:** apply the frame-padding patches in your test setup (see `Topaz.Tests.Python/tests/conftest.py` for a reference implementation). These patches intercept decoded frames and pad them to the full AMQP field count before the client library processes them.

### Planned fix — v1.6-beta

Evaluate upgrading to a newer version of AMQPNetLite that may have resolved these encoding deviations. If no compliant version is available, evaluate replacing AMQPNetLite with a stricter AMQP 1.0 implementation (e.g. [Apache Qpid Proton .NET](https://qpid.apache.org/proton/) or a custom minimal server) that encodes all performative fields up to the defined count, matching what spec-conforming clients expect. The goal is for Service Bus and Event Hubs to work with standard Python, JavaScript, and Go Azure SDK clients without any client-side patches.

---


**Affected services:** Blob Storage, Queue Storage, Table Storage (data plane)

Real Azure validates the `sip` parameter in a Service SAS token against the source IP address of the incoming request. If the caller's IP falls outside the declared range the request is rejected with `403 AuthorizationSourceIPMismatch`.

Topaz detects the `sip` parameter and logs it at debug level but does **not** block any requests based on it. All source IPs are permitted regardless of the `sip` value in the SAS token.

### Impact

Tests or applications that rely on `sip=` to restrict access to a specific IP range will not see requests rejected — the SAS token grants access to all callers as if `sip` were absent.

**Workaround:** none. IP-range enforcement cannot be tested against Topaz in this release.

### Planned fix — v1.7-beta

Implement source-IP extraction from the HTTP context and compare it against the CIDR or single-IP range expressed in `sip`. Return `403 AuthorizationSourceIPMismatch` when the check fails.

---

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

## ARM Deployments — running deployments cannot be cancelled

**Affected services:** Azure Resource Manager (`Microsoft.Resources/deployments`)

Real Azure allows cancelling a deployment that is actively running. The control plane stops provisioning further resources mid-flight and transitions the deployment's `provisioningState` to `Canceled`, leaving any already-provisioned child resources in place.

Topaz's deployment engine is a single background thread that processes one deployment at a time, executing each resource in the template sequentially to completion. There is no cooperative interruption mechanism once a deployment has been dequeued and started. Cancellation is therefore only possible while a deployment is still in the queue — that is, while its `provisioningState` is `Created` and the orchestrator thread has not yet picked it up.

### Impact

Calling `POST .../deployments/{name}/cancel` against a deployment whose `provisioningState` is `Running` returns `409 Conflict`, matching the Azure response for a deployment that has already completed. If the deployment transitions from `Created` to `Running` between the state check and the cancel request, the cancel will also return `409`.

**Workaround:** submit the cancel request immediately after `PUT .../deployments/{name}` to increase the likelihood of hitting the `Created` window. In practice, local deployments are fast enough that this window is narrow; use `provisioningState: Canceled` as a test fixture by cancelling a queued deployment before the orchestrator processes it.

### Planned fix — v1.6-beta

Introduce a cooperative cancellation token into the orchestrator thread so that a cancel request against a `Running` deployment signals the engine to stop processing further resources after the current one completes, matching real Azure mid-flight cancellation semantics.

## ARM Deployments — child resources as standalone entries not supported

**Affected services:** Azure Resource Manager (`Microsoft.Resources/deployments`)

ARM templates allow declaring child resources either **inline** (nested under the parent's `properties`) or as **standalone sibling entries** in the top-level `resources` array using a compound type and name, for example:

```json
{
  "type": "Microsoft.Network/virtualNetworks/subnets",
  "name": "my-vnet/default",
  "dependsOn": ["[resourceId('Microsoft.Network/virtualNetworks', 'my-vnet')]"],
  ...
}
```

Topaz's `TemplateDeploymentOrchestrator` routes each resource entry to a control plane by exact `type` match. Only top-level resource types (e.g. `Microsoft.Network/virtualNetworks`) have registered cases; compound child-resource types (e.g. `Microsoft.Network/virtualNetworks/subnets`) fall through to the `default` branch and are silently skipped with a warning log entry.

This applies to all subresources across all services — subnets, NSG rules, Key Vault access policies declared as child resources, Event Hub consumer groups, etc.

### Impact

Templates that express subresources as standalone entries deploy the parent resource successfully but silently omit the child resources. No error is surfaced to the caller; the deployment still transitions to `Succeeded`.

**Workaround:** use the inline subresource syntax instead of standalone child entries. For example, declare subnets inside `properties.subnets` of the VNet resource rather than as separate `Microsoft.Network/virtualNetworks/subnets` entries.

### No fix planned

## Table Storage — explicit endpoint required in connection strings

**Affected services:** Table Storage (port 8890)

Table Storage data-plane endpoints are served over HTTPS (port 8890) using the Topaz self-signed certificate, which covers `*.table.storage.topaz.local.dev`. However, connection strings must always specify an explicit `TableEndpoint=https://…:8890` component. If you rely on `DefaultEndpointsProtocol=https` alone (without an explicit `TableEndpoint`), the Azure SDK derives the table URL from the default cloud suffix (`core.windows.net`), which does not resolve to the Topaz emulator.

### Impact

Constructing a `TableServiceClient` with only `DefaultEndpointsProtocol=https;AccountName=…;AccountKey=…` will attempt to reach `https://<account>.table.core.windows.net` rather than the Topaz host.

**Workaround:** always use `TopazResourceHelpers.GetAzureStorageConnectionString(...)`, which explicitly sets `TableEndpoint=https://…:8890`.

### Planned fix — v1.6-beta

Covered by the single-port HTTPS consolidation planned for Blob/Table/Queue/File storage (see above).

## Key Vault — `wrapKey`/`unwrapKey` for `oct` keys does not implement RFC 3394 AES Key Wrap

**Affected services:** Key Vault data plane — `wrapKey`, `unwrapKey`

The Azure Key Vault REST API specifies that `wrapKey`/`unwrapKey` operations on `oct` (symmetric) keys use **RFC 3394 AES Key Wrap** — algorithms `A128KW`, `A192KW`, and `A256KW`. These produce a deterministic, padded output with no initialization vector.

Topaz's current implementation dispatches `wrapKey`/`unwrapKey` for `oct` keys to the same AES-GCM and AES-CBC(PAD) code paths as `encrypt`/`decrypt`. At the REST layer the endpoints respond correctly to AES-GCM and AES-CBC(PAD) algorithm names, but the RFC 3394 algorithms (`A128KW`, `A192KW`, `A256KW`) return `400 BadParameter` because Topaz has no RFC 3394 implementation. The `encrypt` and `decrypt` endpoints for `oct` keys are fully supported and unaffected.

### Impact

Code that calls `CryptographyClient.WrapKey(KeyWrapAlgorithm.A256KW, ...)` or `az keyvault key wrap-key --algorithm A256KW` against a Topaz `oct` key will receive a `400 BadParameter` error. Callers that use `CryptographyClient.Encrypt` / `Decrypt` with the same AES-GCM or AES-CBC(PAD) algorithms are fully functional.

**Workaround:** use `CryptographyClient.Encrypt` and `Decrypt` with AES-GCM or AES-CBC(PAD) algorithms (`A256GCM`, `A256CBCPAD`, etc.) instead of `WrapKey`/`UnwrapKey` for symmetric key scenarios.

### No fix planned

Implementing RFC 3394 AES Key Wrap requires a custom in-process implementation (no built-in .NET API exists). This is deferred indefinitely.

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


## Storage Account — secondary endpoint general reads

**Affected services:** Blob Storage, Queue Storage, Table Storage (RA-GRS / RA-GZRS accounts)

For accounts created with `Standard_RAGRS` or `Standard_RAGZRS` SKUs, Topaz registers secondary hostnames (`{accountName}-secondary.*`) and returns correct `secondaryEndpoints` URLs in the ARM response. The `GET ?restype=service&comp=stats` endpoint is fully functional on secondary endpoints for all three services, and mutating operations (PUT, DELETE, POST, PATCH) correctly return `403 WriteOperationNotSupportedOnSecondary`.

However, standard data-plane read operations — downloading blobs, listing containers, reading queue messages, querying table entities — are not routed through the secondary endpoint. Requests to secondary data-plane endpoints other than `?comp=stats` return `404 Not Found` rather than serving data.

### Impact

SDK clients that configure a secondary read policy (e.g., `GeoRedundantReplication.On` in the Azure Storage SDK) and attempt to read data from the secondary endpoint will receive `404` responses.

**Workaround:** avoid enabling secondary-read policies in tests or application code targeting Topaz. Use only the primary endpoint for all data reads.

### Planned fix — v1.6-beta

Route standard GET operations on secondary endpoints to the same in-memory data store as the primary endpoint, making secondary reads fully functional.

## Virtual Network — `CheckIPAddressAvailability` returns empty `availableIPAddresses`

**Affected services:** Virtual Network control plane — `GET .../virtualNetworks/{name}/checkIPAddressAvailability`

The `available` and `isPlatformReserved` fields in the response are computed correctly: `available` is `true` when the queried IP falls within a subnet CIDR registered on the VNet, and `false` otherwise. However, the `availableIPAddresses` field is always an empty array (`[]`).

Real Azure populates `availableIPAddresses` with a list of alternative free IPs when the queried address is already allocated — for example when a NIC or private endpoint has claimed it. This requires a registry of which individual IPs within each subnet are in use. Topaz currently has no such registry: subnets are persisted, but the individual IP addresses assigned to child resources (NICs, private endpoints, service endpoints) are not tracked.

### Impact

`availableIPAddresses` is only meaningful when `available: false` due to an IP being in use. In Topaz, `available: false` only occurs when the IP falls outside all subnet CIDRs — a case where suggestions are practically meaningless anyway. Callers that inspect `availableIPAddresses` to find a next-available address will always receive an empty list.

**Workaround:** if your test logic needs to locate a free IP, pick one explicitly within a known subnet CIDR. Any IP within a subnet is considered available in Topaz (no allocation tracking means no conflicts).

### Planned fix — v1.5-beta

Introduce an IP allocation registry in the VNet service so that NIC and private endpoint creation records their assigned IP address. This will enable real `availableIPAddresses` computation and bring the emulator's `available: false` branch into closer alignment with real Azure.


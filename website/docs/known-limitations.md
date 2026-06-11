---
sidebar_position: 9
description: Known limitations of Topaz — behaviours that differ from real Azure due to deliberate design trade-offs, planned for a future release.
keywords: [topaz limitations, azure emulator limitations, storage ports, topaz known issues]
---

# Known limitations

This page documents deliberate design trade-offs in the current version of Topaz that differ from real Azure behaviour. Each entry notes the impact and the milestone where the limitation is expected to be resolved.

## Entra — ROPC login requires `HTTPS_PROXY` on non-Docker installs

**Affected services:** Entra ID authentication (`az login --username --password`)

MSAL (the authentication library used by Azure CLI and the Azure SDKs) performs a user-realm discovery pre-flight request (`GET https://{authority}/common/userrealm/{username}?api-version=1.0`) before issuing a Resource Owner Password Credentials (ROPC) token. MSAL **always strips the port** from the authority URL when building this discovery URL, so the request is always sent to port **443** regardless of the authority you configured.

On non-Docker installs Topaz does not bind port 443 because doing so requires root/admin privileges. The discovery request therefore hits a port that nothing listens on and fails with `ECONNREFUSED`.

Topaz includes a built-in HTTP CONNECT proxy (port **44380**) that intercepts `CONNECT topaz.local.dev:443` tunnels and forwards them to port 8899, where Topaz's HTTPS endpoint listens. Setting `HTTPS_PROXY` redirects MSAL's discovery request through this proxy and resolves the failure. Non-Topaz `CONNECT` requests (for example Azure CLI telemetry) pass through to the real internet unchanged.

### Impact

Running `az login --username <user> --password <pass>` without `HTTPS_PROXY` set fails on standalone-executable and Homebrew installs with an authentication error or a connection-refused message. Docker users are unaffected — port 443 is bound directly inside the container.

**Workaround:** set `HTTPS_PROXY` before running any `az login --username` command. Topaz prints the exact command when the host starts.

```bash
# macOS / Linux / WSL
export HTTPS_PROXY=http://127.0.0.1:44380
az login --username alice@mytenant.onmicrosoft.com --password P@ssw0rd!
```

### No fix planned

Binding port 443 on Linux and macOS requires elevated privileges (`CAP_NET_BIND_SERVICE` or `sudo`). Requiring root at runtime is a non-goal for Topaz. The built-in CONNECT proxy is the permanent solution for non-Docker installs.

---

## AMQP — AMQPNetLite encoding breaks non-.NET clients

**Affected services:** Service Bus, Event Hubs (AMQP data plane)

Topaz's AMQP server is built on [AMQPNetLite 2.5.1](https://github.com/Azure/amqpnetlite). While AMQPNetLite is fully compatible with .NET Azure SDK clients, it produces AMQP frames that break several non-.NET clients in practice:

1. **Trailing null fields are omitted.** AMQPNetLite encodes AMQP performatives (Attach, Transfer, Flow, Disposition, etc.) by writing only the fields up to the last non-null value and omitting trailing optional fields. This is **explicitly permitted by the AMQP 1.0 specification** (section 1.4). However, the real Azure Service Bus and Event Hubs broker always emits full-length performative lists with every field present — even trailing ones that are null. SDKs were built and tested exclusively against the real broker, so several clients (most notably the Python `_pyamqp` transport used by `azure-eventhub` 5.x) access performative fields by **fixed numeric index** rather than by symbolic name and raise `IndexError` when a frame is shorter than expected.

2. **`Error` objects are encoded with two fields instead of three.** AMQPNetLite encodes an `Error` composite as `[condition, description]`, omitting the optional `info` (third) field. Clients that unconditionally access `error[2]` crash when receiving any Detach or Close frame that carries an error.

In short: Topaz is spec-compliant, but the real Azure broker is more explicit than the spec requires. SDKs were written against the broker's behaviour, not the spec minimum.

### Phase 1 investigation results (v1.6-beta)

Two application-level bugs in Topaz were found and fixed during Phase 1 (missing `MessageId` on received messages; batch message unwrapping for Event Hub batch sends). After those fixes the following baseline was established:

| Client | Service Bus | Event Hubs | Notes |
|---|---|---|---|
| .NET Azure SDK | ✅ Pass | ✅ Pass | Unaffected by frame length |
| Node.js Azure SDK | ✅ Pass | ✅ Pass | Passes without patches after Topaz fixes |
| Python `azure-servicebus` | ⚠️ Patches required | — | `uamqp` requires frame-padding patches in `conftest.py` |
| Python `azure-eventhub` | — | ❌ Fail | `_pyamqp` raises `IndexError` on short performatives |
| Go Azure SDK | ⬜ Not tested | ⬜ Not tested | — |

Upgrading AMQPNetLite from 2.5.1 to 2.5.3 does not resolve the Python incompatibility.

### Impact

Python `azure-eventhub` cannot receive events from Topaz without a server-side fix. Python `azure-servicebus` works only with the frame-padding patches applied in the test harness. .NET and Node.js Azure SDK clients are fully functional without any patches.

**Workaround:** apply the frame-padding patches in your test setup (see `Topaz.Tests.Python/tests/conftest.py` for a reference implementation). These patches intercept decoded frames and pad them to the full AMQP field count before the client library processes them.

### Planned fix — v1.7-beta

Investigate whether AMQPNetLite can be patched — either via a subclass override or a post-encode buffer rewrite — to append trailing null bytes to every performative so the encoded frame length matches the full field count defined by the AMQP 1.0 spec for each performative type (Open, Begin, Attach, Transfer, Flow, Disposition, Detach, End, Close). This would make Topaz's output match the real Azure broker without replacing the entire AMQP stack. If patching is not feasible, evaluate replacing AMQPNetLite with a fully spec-compliant implementation (e.g. [Apache Qpid Proton .NET](https://qpid.apache.org/proton/)) that always encodes explicit nulls. Success criterion: Python `azure-eventhub` and `azure-servicebus` tests pass without any monkey-patching.

---

## Storage SAS — `sip` (source IP) parameter not enforced

**Affected services:** Blob Storage, Queue Storage, Table Storage (data plane)

Real Azure validates the `sip` parameter in a Service SAS token against the source IP address of the incoming request. If the caller's IP falls outside the declared range the request is rejected with `403 AuthorizationSourceIPMismatch`.

Topaz detects the `sip` parameter and logs it at debug level but does **not** block any requests based on it. All source IPs are permitted regardless of the `sip` value in the SAS token.

### Impact

Tests or applications that rely on `sip=` to restrict access to a specific IP range will not see requests rejected — the SAS token grants access to all callers as if `sip` were absent.

**Workaround:** none. IP-range enforcement cannot be tested against Topaz in this release.

### Planned fix — v1.7-beta

Implement source-IP extraction from the HTTP context and compare it against the CIDR or single-IP range expressed in `sip`. Return `403 AuthorizationSourceIPMismatch` when the check fails.

---

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

## ARM Deployments — `reference()` expressions in outputs are not evaluated

**Affected services:** Azure Resource Manager (`Microsoft.Resources/deployments`)

Deployment output values may contain ARM template language expressions including `reference()` to fetch properties of deployed resources. Topaz's template processing engine (Azure SDK's `TemplateDeploymentEngine`) evaluates most common template expressions: `parameters()`, `variables()`, `resourceId()`, `concat()`, and others. However, `reference()` requires a runtime round-trip to the resource provider to read deployed resource state — a capability Topaz has deferred.

When a deployment completes, Topaz serializes the template's `outputs` map directly into the `DeploymentResourceProperties.Outputs` field. Any output value containing a raw `reference()` call (e.g. `"[reference('storageAccountId').primaryEndpoints.blob]"`) will appear as a literal string expression rather than the evaluated property value.

### Impact

Deployments with output values that use `reference()` will see those outputs returned as unevaluated expression strings instead of actual resource property values. Callers that read `deployment.Properties.Outputs` will receive the template syntax (e.g. `"[reference(...)]"`) rather than resolved data.

Deployments with outputs using only `parameters()`, `variables()`, `resourceId()`, `concat()`, `uniqueString()`, or literal values work correctly.

**Workaround:** avoid using `reference()` in deployment outputs. Refactor templates to output only `resourceId()` or literal values, and have the caller fetch resource properties directly if needed.

### Planned fix — v1.8-beta

Extend `TemplateDeploymentOrchestrator.RouteDeployment` to collect all `reference()` calls from the outputs map, resolve them by reading from the respective control planes, and substitute the evaluated values back into the outputs before calling `SetOutputs()`. This requires mapping template resource types to their control planes, similar to the existing resource routing logic.

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


## Storage Account — secondary endpoint reads return primary data

**Affected services:** Blob Storage, Queue Storage, Table Storage (RA-GRS / RA-GZRS accounts)

For accounts created with `Standard_RAGRS` or `Standard_RAGZRS` SKUs, Topaz routes all read operations on `{accountName}-secondary.*` endpoints to the same in-memory data store as the primary endpoint. The secondary endpoint is fully reachable and returns real data — no 404s — but there is no actual replication process running in the background. Reads on the secondary are always perfectly in sync with the primary, which does not model the real-world replication lag of geo-redundant storage.

Additionally, the `<LastSyncTime>` element returned by `GET ?restype=service&comp=stats` on secondary endpoints is set to the current wall-clock time rather than to a persisted scheduler tick, so it does not reflect a meaningful replication checkpoint.

### Impact

Tests or applications that rely on observing eventual-consistency behaviour (stale reads on secondary, `LastSyncTime` lagging behind writes) cannot be validated against Topaz in this release.

**Workaround:** none. Secondary reads will always reflect the latest primary state.

### Planned fix — v1.9-preview

Introduce a `GeoReplicationSyncScheduler` background service that periodically updates a persisted `LastGeoSyncTime` field on each RA-GRS/RAGZRS account and threads it through the service-stats XML responses. This will make `<LastSyncTime>` reflect a realistic scheduler tick rather than wall-clock time, simulating replication lag without requiring a real secondary data store.

---

## Python SDK (`topaz-sdk`) — `REQUESTS_CA_BUNDLE` required for SSL trust

**Affected services:** All Topaz HTTP/HTTPS endpoints accessed via the Python SDK (`topaz-sdk`) or any Python Azure SDK client pointed at Topaz.

The Topaz self-signed TLS certificate is not included in the default Python (`certifi`) trust store. Any HTTPS request made without explicit certificate configuration will raise an `SSLError` with a certificate verification failure.

The `topaz-sdk` `TopazArmClient` is aware of this: when `REQUESTS_CA_BUNDLE` is set in the environment it automatically passes the path to the underlying `requests` session, so requests to Topaz are trusted without any extra code.

### Impact

Python clients that construct raw `requests` sessions, Azure SDK clients (e.g. `azure-keyvault-secrets`, `azure-storage-blob`), or any library that does not honour `REQUESTS_CA_BUNDLE` automatically will raise an `SSLError` when connecting to Topaz endpoints.

**Workaround (recommended):** export the environment variable before running your tests or application:

```bash
export REQUESTS_CA_BUNDLE=/path/to/topaz.crt
```

Alternatively, pass the certificate path explicitly when constructing SDK clients that accept an `ssl_verify` or `verify` parameter.

When using the `topaz-sdk` `TopazArmClient`, setting `REQUESTS_CA_BUNDLE` is sufficient — no additional configuration is needed.

### No fix planned

This is a standard TLS trust-store configuration requirement and not a Topaz bug. Bundling the certificate into the Python trust store is outside the scope of the emulator. Use the `REQUESTS_CA_BUNDLE` workaround described above.


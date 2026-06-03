---
slug: development-updates-06
title: "Topaz Weekly Pulse #6: Azure SQL, Service Bus AMQP data plane, Blob User Delegation SAS, Entra Device Code, ACR Tasks and Runs, and a Python SDK"
authors: topaz
tags: [general, storage, entra, service-bus, arm]
---

*This week in Topaz: Azure SQL arrives as a full first-class service with Servers and Databases. Service Bus gains a real AMQP data plane with queue management and message locking. Blob Storage completes the User Delegation SAS flow. Entra ID adds Device Code and form_post support. Container Registry grows Tasks and Runs automation APIs. And Topaz ships its first Python SDK.*

{/* truncate */}


## Case 1: Azure SQL — Servers and Databases

Azure SQL is the largest new service this week. Both resource tiers are live and wired end-to-end:

**SQL Servers**:
- **Create or Update**, **Get**, **Delete**, **List by resource group**, and **List by subscription**.
- **Connection Policy** — `GET` and `PUT` the server-level connection policy, persisted as an `ArmSubresource<T>`.
- **Vulnerability Assessment** — `GET`, `PUT`, and `DELETE` the server-level vulnerability assessment resource.
- **Transparent Data Encryption** — `GET` and `PUT` the TDE configuration, returning the expected `transparentDataEncryption` model shape.
- CLI commands: `sql server create`, `sql server show`, `sql server list`, `sql server delete`.

**SQL Databases**:
- **Create or Update**, **Get**, **Delete**, and **List by server** — the core database lifecycle.
- **Security Alert Policy** — `GET` and `PUT` the database-level alert configuration.
- **Backup retention policies** — both short-term (`shortTermRetentionPolicies`) and long-term (`longTermRetentionPolicies`) `GET` and `PUT` endpoints return the correct resource shape.
- **Restorable dropped databases** — `GET` list by server, needed by tooling that probes for soft-deleted resources before re-creating.
- `sku` defaults are applied when the request omits the SKU block, preventing tooling from receiving an unrecognised model shape.
- `kind` and `readScale` properties are populated so the Azure CLI and Terraform provider deserialise the response without attribute errors.
- CLI commands: `sql database create`, `sql database show`, `sql database list`, `sql database delete`.

ARM deployment support is wired through `TemplateDeploymentOrchestrator` for both `Microsoft.Sql/servers` and `Microsoft.Sql/servers/databases`, making Terraform and Bicep plans that provision SQL infrastructure work against Topaz out of the box.


## Case 2: Service Bus AMQP data plane

Previous Topaz releases included Service Bus control-plane CRUD. This week the data plane arrived — the AMQP layer that real message-passing code talks to:

**Queue management over AMQP**:
- Dedicated management endpoints handle `PUT` and `DELETE` for queues via the AMQP management link, so broker-level operations (creating a queue from the AMQP client rather than the ARM API) now work.
- `IncomingLinkEndpoint` and `OutgoingLinkEndpoint` both accept a configurable target address, enabling the correct routing of messages sent to named queues.

**Message operations**:
- **Send** — producers attach to the outgoing link and deliver messages; the broker acknowledges with the standard AMQP `accepted` disposition.
- **Receive** — consumers attach to the incoming link and drain messages in FIFO order.
- **Renew lock** — the broker handles the `com.microsoft:renew-message-lock` operation, resetting the lock expiry on an in-flight message and returning the new expiry time. This is the path taken by long-running message processors that use `RenewMessageLockAsync`.
- **Delivery tag format** — uses GUID-based delivery tags, matching the format emitted by the Azure Service Bus .NET SDK so clients can correlate sent and received frames without format errors.

A multi-message consumption test was added, verifying that a single receiver can drain a batch of messages from a queue and that each message is correctly acknowledged.


## Case 3: Blob User Delegation SAS — end-to-end

Storage's SAS enforcement work that started last week is extended this week with full User Delegation Key and User Delegation SAS support for Blob data-plane operations:

**User Delegation Key generation**:
- `POST /subscriptions/.../storageAccounts/{account}/blobServices/default/generateUserDelegationKey` — accepts a `KeyInfo` payload with `signedStart` and `signedExpiry`, generates a 256-bit signing key, and returns it in the standard response shape.
- Keys are stored via the resource provider and expire automatically after the requested window.

**User Delegation SAS validation**:
- Incoming SAS tokens carrying `skoid`, `sktid`, `skt`, `ske`, and `skv` parameters are identified as user delegation tokens.
- The token's canonical string-to-sign is reconstructed using the stored delegation key and validated with HMAC-SHA256, exactly matching Azure's signing algorithm.
- Permission (`sp=`), resource (`sr=`), and expiry (`se=`) are enforced after the signature check passes.

**Blob service properties**:
- `GET /?restype=service&comp=properties` — returns the service-level properties document (CORS rules, logging, metrics, soft delete settings) as XML.
- `PUT /?restype=service&comp=properties` — persists the updated properties document, allowing tests that configure CORS or enable soft delete to verify their writes immediately.

This completes the SAS enforcement story for the Blob plane — applications that use signed URLs, delegation keys, or stored access policies can now run their auth logic against Topaz without a live Azure subscription.


## Case 4: Entra ID — Device Code flow and form_post

The Entra ID emulation grew two new authentication flows this week:

**Device Code flow** (`/oauth2/v2.0/devicecode` + `/oauth2/v2.0/token`):
- `POST /devicecode` — returns the standard `device_code`, `user_code`, `verification_uri`, and `expires_in` fields.
- Polling `POST /token?grant_type=urn:ietf:params:oauth:grant-type:device_code` with the device code eventually resolves to an access token, mirroring the Azure AD device-code polling contract.
- This unblocks tooling and headless scripts that use the device-code grant to authenticate before performing management operations.

**`form_post` response mode** in the authorisation endpoint:
- When `response_mode=form_post` is included in an authorisation request, Topaz now returns a self-submitting HTML form carrying the `code` and `state` parameters instead of a redirect — the response mode used by confidential web app flows (OpenID Connect `response_type=code`).
- End-to-end tests covering the form rendering and parameter extraction were added.


## Case 5: ACR Tasks and Runs

The Container Registry service gained full automation API coverage this week, adding the two resource types that CI/CD pipelines and build agents rely on:

**Tasks** (`/tasks`):
- **Create or Update**, **Get**, **Update** (PATCH), **Delete**, and **List** per registry.
- The `AcrTaskResource` model includes the platform (`os`, `architecture`), trigger configuration, and step definitions (Docker, File, EncodedTask).

**Runs** (`/runs`):
- **Schedule a run** — `POST /registries/{name}/runs` queues an immediate run; the endpoint returns an `AcrRunResource` with status `Queued`.
- **Run a task** — `POST /registries/{name}/scheduleRun` invokes a named task and links the resulting run to it.
- **Get**, **Update** (cancel/patch status), and **List** run records per registry.
- **Log SAS URL** — `GET /runs/{runId}/listLogSasUrl` returns a pre-signed URL for the run log, following the ACR REST contract.
- **Log content** — `GET /runs/{runId}/log` and `HEAD /runs/{runId}/log` serve the log body (empty for emulated runs) with the correct content-type headers that the ACR SDK expects.

Teams building container build pipelines with Azure Container Registry Tasks can now write Terraform or Bicep that provisions Tasks and Records, schedule test runs against Topaz, and verify the full resource lifecycle locally.


## Case 6: Python SDK — `topaz-sdk`

Topaz now ships an official Python SDK (`topaz-sdk`), published to PyPI. It gives Python test suites and automation scripts the same first-class access to Topaz that the .NET Azure SDK has had since the beginning:

**`TopazArmClient`** — a thin HTTP client that mirrors the C# `TopazArmClient`:
- `create_subscription()`, `create_resource_group()`, `delete_resource_group()` — the lifecycle operations needed to set up and tear down test fixtures.
- Automatic `Bearer` token injection via the bundled `AzureLocalCredential`.
- SSL verification honours `REQUESTS_CA_BUNDLE`, so the Topaz TLS certificate is trusted automatically inside the Docker-based test container.

**`AzureLocalCredential`** — wraps the Topaz identity endpoint and caches tokens until expiry, providing a drop-in replacement for `DefaultAzureCredential` in local-only test environments.

**`TopazEnvironment`** — a helper class that resolves Topaz endpoint URLs (resource manager, blob storage, key vault, etc.) from environment variables, removing the need for every test file to hard-code port numbers.

A CI workflow that builds, tests, and publishes the package to PyPI was added alongside the SDK source. Python test suites in `Topaz.Tests.Python` now use the SDK for fixture setup, replacing the ad-hoc `requests` calls that existed before.

:::tip[Try Topaz with a single command]
Everything runs in a single binary — no Azure subscription required.

```bash
brew tap thecloudtheory/topaz && brew install topaz && topaz-host   # macOS
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/main/install/get-topaz.sh | bash   # Linux
```

Install the Python SDK:

```bash
pip install topaz-sdk
```

[Getting started →](https://topaz.thecloudtheory.com/docs/intro/) · Not ready to install? [Star the repo →](https://github.com/TheCloudTheory/Topaz)
:::

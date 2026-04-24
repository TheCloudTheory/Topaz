---
slug: development-updates-02
title: "Topaz Weekly Pulse #2: Key Vault keys, Queue Storage, and ARM deployments go deep"
authors: kamilmrzyglod
tags: [general, keyvault, storage, arm]
---

*This week in Topaz: cryptographic key lifecycle, a brand-new Queue service, and ARM deployments that finally feel real.*

{/* truncate */}

## Case 1: Key Vault cryptographic key operations are now fully covered

Last week Key Vault had secrets. This week it gained a complete key management surface:

- **Create**, **Get**, **List**, and **List Versions** for keys.
- **Import** keys from external material.
- **Update** key attributes and **Delete** keys.
- **Backup** and **Restore** a key (cross-vault portability).
- **Recover** and **Purge** soft-deleted keys.
- **Rotate** a key on demand.
- **Get** and **Update** a key rotation policy.

Every endpoint is wired to the CLI with a matching command. Authorization enforcement was also tightened: all Key Vault data-plane endpoints now require a valid token, closing a gap that existed since the service launched. For teams that rely on `DefaultAzureCredential` + Key Vault in local tests, this brings the emulator much closer to production behavior.

## Case 2: Queue Storage arrived

Queue Storage is a new service in Topaz this week. The first iteration covers the core message-passing flow:

- Queue creation, deletion, and listing.
- **PutMessage** — enqueue a message with optional TTL and visibility timeout.
- **GetMessages** — dequeue one or more messages.
- **PeekMessages** — inspect without locking.
- **DeleteMessage** and **ClearMessages**.
- Queue metadata and properties endpoints.

The service runs over HTTPS on the shared Storage port and integrates with the existing Storage account model. This closes a meaningful gap for workloads that use Azure queues for background jobs or async workflows — they can now develop and test end-to-end locally without a real Azure subscription.

## Case 3: ARM deployment management is now a first-class surface

The resource manager gained a complete deployment management API this week, covering both resource-group scope (which existed before) and the subscription scope (new):

- **Create / Update / Get / Delete / List** deployments at both scopes.
- **Validate** a deployment template before applying it.
- **Cancel** a running deployment mid-flight.
- **Export** a deployment template from an existing deployment.

The What-If operation was also added — submit a template and get back a prediction of what would change, without actually deploying. This is widely used by Terraform's plan phase and by `az deployment what-if`. End-to-end tests covering What-If via Azure CLI were added alongside the implementation.

## Case 4: Management Groups core CRUD landed

Management Groups are now a supported resource type:

- **Create**, **Get**, **Update**, and **Delete** management groups.
- **List** management groups under a tenant.
- **Management group scoped deployments** — deploy ARM templates targeting a management group, with validate and cancel support.

This matters for teams building policy-as-code or governance tooling that targets management group scopes. The service follows the same control plane + endpoint pattern as the rest of Topaz, and is registered in the ARM deployment orchestrator.

## Case 5: Table Storage correctness and protocol fixes

Several edge cases in Table Storage were resolved:

- **OData single-table lookup** (`GET /Tables('{name}')`) was missing; it's now implemented. SDKs that verify a table exists before operating on it will no longer fail silently.
- **URL-encoded spaces in `RowKey`** — entity endpoint routing now correctly decodes percent-encoded characters before matching, fixing a class of 404 errors on rows with spaces in the key.
- **URL-decode in the signing path** — the Storage security provider now URL-decodes resource paths before computing SharedKey signatures, aligning with Azure's canonicalization rules. The CLI test suite was updated with matching SharedKeyLite signature computation to validate the fix end-to-end.

## Case 6: Blob Storage — UndeleteBlob added

Soft-deleted blobs can now be recovered with the `UndeleteBlob` operation. The endpoint, CLI command, and integration tests were all added together. This is the expected companion to the existing soft-delete support and is needed by any lifecycle policy that uses blob versioning with restore.

## Case 7: CLI command discoverability improved

All CLI commands that were missing `[CommandDefinition]` attributes now have them. The attribute drives help text generation and command listing. The practical effect is that `topaz --help` and service-specific help trees are now complete — no silent gaps where a command existed but wasn't described.

## Case 8: Linux installer added

A shell installer for Linux (`install-linux.sh`) was added to the `install/` directory, matching the existing macOS installer. This makes it simpler to bootstrap Topaz on Linux CI machines or developer workstations without manually placing binaries.

## What to expect next

Key Vault certificate operations are on the roadmap and partially scoped in the backlog already. Queue Storage will likely grow with storage account SAS support. The ARM deployment surface will continue toward full parity with the Azure Resource Manager REST API.

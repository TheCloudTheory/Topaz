---
slug: development-updates-01
title: "Topaz Weekly Pulse #1: Major Storage milestones and cleaner automation workflows"
authors: kamilmrzyglod
tags: [general, storage, cicd]
---

*This week in Topaz: from page blobs to health-checked host orchestration.*

This is the first post in a new weekly series: **Topaz Ship Log**.

Each edition is a concise, case-by-case summary of what changed in Topaz during the week, why it matters, and what it unlocks for local Azure development. This first issue covers the last 7 days of work.

{/* truncate */}

## Case 1: CLI and Host are now properly split

Topaz now runs as two separate binaries with a clear contract:

- `topaz-host` runs the emulator process.
- `topaz` runs short-lived management commands.

The Host exposes `GET /health`, and the CLI performs a pre-flight health check before command execution, including validation that both processes use the same working directory. That removes a whole class of confusing "wrong state" failures when scripting across multiple projects.

For automation this is a big quality-of-life improvement: run the host as a service/container, then call CLI commands independently.

## Case 2: Azure Storage control plane coverage grew substantially

This week added a large set of Storage account management capabilities, including:

- Storage account management endpoints and commands.
- Listing storage accounts by subscription.
- Name availability checks.
- Account key regeneration.
- Account SAS and Service SAS listing.
- Storage account update (`PATCH`) support.
- Default endpoint shaping for blob/file/queue/table services.

This closes many of the "day one" gaps for provisioning and managing storage resources with Azure-like workflows.

## Case 3: Blob container operations moved much closer to Azure behavior

Container-level behavior was expanded with support for:

- Metadata set/get.
- ACL set/get.
- Lease lifecycle operations (`acquire`, `renew`, `change`, `release`, `break`).
- Container properties retrieval.

Combined with related test updates, this improves compatibility for tooling that depends on real ACL and lease semantics instead of simplified placeholders.

## Case 4: Blob data plane added key block-blob capabilities

Topaz now supports the core multi-step block-blob flow:

1. Stage blocks (`Put Block`).
2. Commit block list (`Put Block List`).
3. Inspect committed/uncommitted blocks (`Get Block List`).

On top of that, the week included:

- Blob copy operations.
- Blob metadata retrieval.
- Blob properties updates.
- Header correctness fixes (for example `Cache-Control` handling).
- A broader refactor to use operation result abstractions for more consistent error handling paths.

These changes make SDK and CLI integration paths much more predictable for real-world blob workflows.

## Case 5: Page blob support arrived (including page ranges)

Page-blob scenarios were introduced with support for:

- Creating page blobs.
- Uploading pages.
- Reading page blob content.
- Retrieving page ranges.

This is important for workloads and tests that use page-oriented behavior rather than block blobs.

## Case 6: Table Storage data plane expanded and protocol alignment improved

Table Storage saw a broad set of updates:

- Table management and entity operations.
- Service properties and stats endpoints.
- CORS preflight handling.
- ACL-related updates.
- Endpoint permission adjustments.
- HTTPS protocol alignment for table endpoints.

The net effect is better fidelity for clients that are strict about protocol and service-level behavior.

## Case 7: Auth and request handling hardening

Several cross-cutting correctness improvements landed:

- Authorization checks now include HTTP method in permission evaluation.
- Storage account key generation moved to base64 encoding.
- Router/query parsing and endpoint ordering/path handling fixes for edge cases.

These are not headline features, but they materially reduce subtle interoperability bugs.

## Case 8: Faster and more reliable test loops

A lot of work this week was aimed at development speed and CI reliability:

- Terraform test infrastructure improvements (`AzureRmBatchFixture`, provider pre-initialization).
- Docker-based test setup hardening and host mapping fixes.
- Better diagnostics and output handling in tests.
- Log truncation safeguards to prevent oversized test logs.
- New/updated workflows (including portal build/test/publish paths).

The practical outcome is shorter feedback loops and easier debugging when compatibility regressions appear.

## Case 9: Contribution and documentation improvements

The project also gained process and docs upgrades:

- New issue templates and a pull request template.
- Expanded docs for local Terraform and ecosystem integration.
- Clarifications around CLI/Host usage and command expectations.
- Roadmap/backlog updates for upcoming storage and identity work.

These changes make it easier for new contributors to get aligned and for users to understand current feature boundaries.

## Case 10: Cross-service API coverage moved forward too

Although Storage dominated the week, several other services also progressed:

- **Microsoft Entra ID**: tenant-specific token endpoints and group management endpoints were added.
- **Key Vault**: deleted vault recovery and purge flows were expanded and aligned with expected status semantics.
- **Service Bus**: namespace model handling improved and network rule set support was introduced.
- **Container Registry**: repository/tag/blob delete paths, repository listing, replication listing, and HEAD/manifest behavior fixes improved data-plane compatibility.

This matters because most real test environments are multi-service. Improving only one API surface is never enough; the weekly work intentionally moved several surfaces in parallel.

## What to expect next

`Topaz Ship Log` will continue weekly with the same format: major changes, grouped by area, focused on practical impact.

The current direction is clear: deeper Azure Storage parity, tighter CLI/SDK behavior, and continued investment in test-backed compatibility for local-first infrastructure development.

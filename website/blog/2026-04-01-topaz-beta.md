---
slug: topaz-beta
title: Topaz is now in beta
authors: kamilmrzyglod
tags: [general]
---

After months of alpha development, Topaz is moving to beta. The core emulation layer is stable, the service catalogue has grown considerably, and the rough edges that made early adopters cautious are largely gone. This post walks through what changed and what is currently available.

<!-- truncate -->

## What "beta" means for Topaz

The alpha label was a signal that APIs, file layouts, and internal protocols were still in flux. Breaking changes happened between releases without ceremony. Starting with this release we are committing to a few things:

- **Stable storage layout.** The `.topaz/` directory structure and the on-disk resource format are frozen. Existing state created by an alpha build will be migrated automatically on first run.
- **No surprise breaking changes.** Any removal of a supported endpoint will appear in the release notes at least one release in advance.
- **Versioned REST coverage.** Each service page in the docs now tracks exactly which operations are implemented so you know what to expect before writing code against Topaz.

The beta label is not a promise of 100% parity with Azure — that goal is by definition unreachable. It is a promise that what is documented as working actually works and will continue to work.

## What is available today

### Microsoft Entra ID

Topaz ships a full Entra emulation layer out of the box. A local tenant (`topaz.local.dev`, tenant ID `50717675-3E5E-4A1E-8CB5-C62D8BE8CA48`) is provisioned at startup with a pre-configured superadmin account. Every token Topaz issues is a real, signed JWT — nothing is stubbed or bypassed.

Supported grant types: `client_credentials`, `password`, `authorization_code`, and `refresh_token`. A local Microsoft Graph API covers users, applications, service principals, and groups. The OIDC discovery endpoint is fully functional.

Read more in the [Entra ID emulation post](/blog/entra-id-emulation).

### Azure Key Vault

The most complete service in Topaz. The control plane exposes the full vault lifecycle (create, update, delete, soft-delete, purge) and access-policy management. The data plane covers secrets, keys, and certificates on port 8898.

### Azure Storage

Blob Storage (port 8891) and Table Storage (port 8890) are both available. Storage accounts can be created via ARM and accessed with either connection-string credentials or `DefaultAzureCredential`. SAS token generation and container-level operations are supported.

### Azure Service Bus

Namespaces, queues, topics, subscriptions, and rules can be created and managed via the ARM control plane. The data plane runs a real AMQP 1.0 broker on ports 8889 (plain) and 5671 (TLS), so any SDK that speaks AMQP works without modification — no mocking, no in-memory shim.

### Azure Event Hubs

Namespace and Event Hub creation are supported via the ARM control plane. The data plane accepts events over AMQP (port 8888) and HTTP (port 8897).

### Managed Identity

User-assigned and system-assigned managed identities are fully supported, including federated identity credentials. The same `DefaultAzureCredential` flow that works in production works locally — no special credential type is required.

### RBAC / Authorization

Role assignments and role definitions are implemented at any ARM scope. The built-in Azure roles (Owner, Contributor, Reader, and the service-specific data-plane roles) are pre-loaded, so you can assign them without any setup.

### Azure Resource Manager

ARM template deployments work end-to-end: `az deployment group create` with a Bicep or JSON template provisions resources in Topaz the same way it would in Azure. Resource groups and subscriptions are also managed locally.

### Container Registry

Registry resources can be created and managed via the ARM control plane. The data plane (OCI / Docker Registry HTTP API — `docker push`, `docker pull`) is not yet available and will be included in an upcoming release.

### Virtual Network

Basic virtual network and subnet resources can be created and managed, which is useful for ARM templates that declare networking dependencies alongside other resources.

## Ecosystem integrations

Beyond the raw Azure SDK compatibility, Topaz integrates with the tools you already use:

- **Azure CLI** — register Topaz as a cloud environment with `az cloud register` and run `az` commands against it directly. See the [Azure CLI integration guide](/docs/azure-cli-integration).
- **ASP.NET Core** — the `AddTopaz()` extension provisions local infrastructure at application startup. See the [ASP.NET Core integration guide](/docs/ecosystem/aspnet-core).
- **Terraform / Bicep / ARM templates** — the Resource Manager emulation understands ARM deployments, so infrastructure-as-code workflows run unchanged.
- **Docker** — a pre-built container image is published so Topaz can run as a sidecar or inside CI pipelines without installing anything on the host.
- **MCP server** — a Model Context Protocol server is included (`Topaz.MCP`) for AI-assisted development workflows.

## What is next

The beta period will focus on filling in the gaps visible in the API coverage pages: subscription-level list operations, Event Hub consumer groups, Service Bus authorization rules, and broader Blob Storage feature coverage are the near-term priorities. Feedback and pull requests are welcome.

Check the API coverage docs (Key Vault, Storage, Service Bus, Event Hubs, and more) for a live view of what each service supports.

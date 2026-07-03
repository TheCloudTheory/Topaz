---
sidebar_position: 1
description: How Topaz intercepts Azure API calls using DNS and TLS, runs the ARM control plane alongside service data planes, and coordinates the two-binary architecture.
keywords: [topaz architecture, how topaz works, azure emulator internals, topaz dns, topaz tls, topaz arm control plane]
---

# How Topaz works

Topaz runs as a local process that intercepts Azure API traffic by combining DNS resolution, TLS termination, and a full implementation of the Azure REST APIs. Understanding how these pieces fit together helps explain why the setup involves DNS configuration and certificate trust, and what happens when you run `topaz-host`.

## The interception model

Azure clients — SDKs, CLI, Terraform providers, custom HTTP clients — address Azure services by hostname. A Key Vault secret is fetched from `https://myvault.vault.azure.net`. A blob is written to `https://mystorageaccount.blob.core.windows.net`. Container images are pushed to `myregistry.azurecr.io`.

Topaz replaces these hostnames at the DNS level. Instead of resolving to Azure's IP addresses, they resolve to `127.0.0.1`. Topaz then terminates the HTTPS connection using a wildcard certificate it bundles, and handles the request itself.

The result: the client code doesn't change. The SDK call you write for a real Azure vault works identically against Topaz. The only visible difference is the hostname — Topaz uses a `*.topaz.local.dev` domain family rather than `*.azure.net`.

## Two executables, two roles

Topaz ships as two separate binaries:

| Binary | Role |
|---|---|
| `topaz-host` | The emulator — runs the ARM control plane and all service data planes. Start this first and leave it running. |
| `topaz` | The CLI — manages resources inside the running emulator. Communicates with `topaz-host` over HTTP. |

`topaz-host` is the long-running server. Every other operation — creating a storage account, listing Key Vaults, checking health — goes through `topaz`.

`topaz` verifies that `topaz-host` is running and that both are in the same working directory before executing any command. This check prevents accidental cross-project interference if you run multiple projects side by side.

## What runs on which port

`topaz-host` listens on several ports simultaneously, one per protocol or service category:

| Port | Purpose |
|---|---|
| 8899 | ARM control plane (`management.azure.com` equivalent) and Entra ID — HTTPS |
| 8898 | Key Vault data plane — HTTPS |
| 8897 | Event Hub HTTP — HTTPS |
| 8895 | Cosmos DB data plane — HTTPS |
| 8892 | Container Registry data plane — HTTPS |
| 8891 | Azure Storage data plane (Blob, Queue, Table, File) — HTTPS |
| 8889 | Service Bus AMQP |
| 8888 | Event Hub AMQP |
| 8887 | Service Bus additional — HTTPS |
| 5671 | AMQP over TLS (enabled when a certificate is provided) |
| 44380 | HTTP CONNECT proxy (required for ROPC `az login`) |

Each Azure service SDK and CLI tool is pre-configured to address a specific hostname and port. The DNS setup ensures those hostnames all resolve to `127.0.0.1`, so traffic arrives at the right port on localhost.

## ARM control plane and data planes

Topaz implements two distinct layers of the Azure API surface:

- **ARM control plane** — the management layer at `management.azure.com`. This is where resource groups are created, storage accounts are provisioned, and Key Vaults are registered. Terraform uses this layer exclusively.
- **Data planes** — the service-specific APIs. Once a storage account exists (ARM), the SDK reads and writes blobs via the storage data plane endpoint (`*.blob.core.windows.net`).

Most Azure emulators only implement one or the other. Topaz implements both, which is what allows a full `terraform apply` to work: the provider calls ARM to create resources, then your application code calls the data plane to use them.

See [Control plane and data plane](./control-plane-and-data-plane.md) for a deeper discussion of the distinction.

## A single process replaces a tool stack

A typical local Azure development setup before Topaz involved combining multiple tools:

- Azurite for Blob, Queue, and Table Storage
- A third-party mock or no emulator for Key Vault
- The real Service Bus because no alternative existed
- Manual configuration glue to make SDKs and Terraform hit the right endpoints

Topaz replaces this with one command:

```bash
topaz-host --default-subscription 00000000-0000-0000-0000-000000000001
```

All services start together, share the same subscription and resource group model, and use the same certificate and DNS configuration.

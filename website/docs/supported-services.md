---
sidebar_position: 4
description: Overview of Azure services supported by Topaz, including Azure Storage, Key Vault, Service Bus, Event Hubs, Container Registry, Managed Identity, and ARM template deployments.
keywords: [supported azure services, azure storage emulator, key vault emulator, service bus emulator, event hub emulator, arm template, bicep, managed identity]
---

# Supported services
Depending on the version of Topaz a different set of Azure services is supported for emulation. The table below presents the current state of emulation and maturity of certain features.

:::tip[Best practice]

Make sure you're using the most recent version of Topaz to benefit from the bugfixes and newest features.

:::

Service Name|Control Plane|Data Plane
------------|-------------|----------
Subscriptions|✅|N/A
Resource Groups|✅|N/A
Azure Storage|✅|✅
Table Storage|✅|✅
Blob Storage|✅|✅
Queue Storage|✅|✅
Key Vault|✅|✅
Event Hub|✅|🚧
Service Bus|✅|🚧
Virtual Network|✅|N/A
Network Interface|✅|N/A
Azure Resource Manager|🚧|N/A
Managed Identity|✅|N/A
Container Registry|✅|🚧
Azure SQL|✅|:x:
Azure App Service|✅|N/A
Azure Virtual Machines|🚧|N/A
Entra ID|N/A|🚧
RBAC|✅|N/A
Monitor|🚧|:x:
Cosmos DB|✅|✅
Azure Disk|✅|🚧
Azure Load Balancer|✅|N/A
Public IP Address|✅|N/A
Azure App Configuration|✅|🚧
Log Analytics|✅|🚧
Application Insights|✅|🚧

✅ - fully supported (stable)

🚧 - partially supported (unstable)

🔜 - coming soon

:x: - not supported

`N/A` - not provided by Azure

:::tip[Looking for a detailed breakdown?]

The [API Coverage](../api-coverage/container-registry) section lists every REST operation for each service and shows exactly which ones are implemented in Topaz. Coverage for Subscriptions and Resource Groups is tracked under the [Azure Resource Manager](../api-coverage/resource-manager) page.

:::

## Used ports
The ports used by Topaz can be divided into two groups:
* common port for Azure Resource Manager operations
* service-specific port for data plane

You can find which service uses which port below:
Service Name|Port|Protocol
------------|----|--------
Resource Manager|8899, 443|HTTPS
Blob Storage|8891|HTTPS
Table Storage|8891|HTTPS
Queue Storage|8891|HTTPS
Azure Key Vault|8898, 443|HTTPS
Azure Event Hub|8897|HTTPS
Azure Event Hub (AMQP)|8888|AMQP
Azure Service Bus|8887, 8899|HTTPS
Azure Service Bus (AMQP)|8889, 5671|AMQP, AMQP/TLS
Container Registry (control plane)|8899|HTTPS
Container Registry (data plane)|8892|HTTPS
Azure App Configuration (data plane)|8893|HTTPS
Azure App Service (Kudu/SCM)|8896|HTTPS
Azure App Service (forward proxy)|8900|HTTPS

:::note[Unified storage port]

All Azure Storage sub-services (Blob, Table, Queue, File) share a single port (`8891`). Requests are routed to the correct sub-service by the second DNS label in the `Host` header (e.g. `{account}.**blob**.storage.topaz.local.dev`, `{account}.**table**.storage.topaz.local.dev`).

:::

For HTTPS endpoints, if you're running Topaz as a standalone application, you need to install and trust the certificates provided along with the main package.

:::note[No sudo required — HTTP CONNECT proxy handles port 443]

When running Topaz as a standalone executable on Linux and macOS, you don't need `sudo` to start the host. Topaz launches a built-in HTTP CONNECT proxy on port `44380` that intercepts Azure CLI requests bound for port 443 and routes them through the emulator.

This is automatic — just set the `HTTPS_PROXY` environment variable before running Azure CLI commands:

```bash
export HTTPS_PROXY=http://127.0.0.1:44380
az login --username alice@mytenant.onmicrosoft.com --password P@ssw0rd!
```

The proxy passes through all non-Topaz requests to the internet unchanged. If you are only using the Azure SDK (not the Azure CLI), you can point your SDK client directly at port `8898` and skip port 443 entirely.

:::

## Ready to try it?

Get Topaz running in minutes — no account or registration needed.

```bash
brew install topaz
```

Or pull the Docker image:

```bash
docker pull thecloudtheory/topaz-host:latest
```

See the [Getting Started guide](/docs/intro/) for full setup instructions, including certificate installation and Azure CLI integration.

:::tip[Missing a service?]

If a service you need isn't listed above or the coverage isn't deep enough for your use case, [open a GitHub Discussion](https://github.com/TheCloudTheory/Topaz/discussions) — your workflow will directly influence what gets built next.

:::
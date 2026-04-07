---
sidebar_position: 2
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
Subscriptions|🚧|N/A
Resource Groups|✅|N/A
Azure Storage|🚧|🚧
Table Storage|🚧|✅
Blob Storage|🚧|🚧
Queue Storage|🔜|🔜
Key Vault|✅|🚧
Event Hub|🚧|🚧
Service Bus|🚧|🚧
Virtual Network|🚧|N/A
Azure Resource Manager|🚧|N/A
Managed Identity|✅|N/A
Container Registry|✅|🚧
Azure SQL|🔜|🔜
Azure Virtual Machines|🔜|N/A
Entra ID|N/A|🚧
RBAC|🚧|N/A
Monitor|🚧|:x:

✅ - fully supported (stable)

🚧 - partially supported (unstable)

🔜 - coming soon

:x: - not supported

`N/A` - not provided by Azure

:::tip[Looking for a detailed breakdown?]

The [API Coverage](./api-coverage/container-registry) section lists every REST operation for each service and shows exactly which ones are implemented in Topaz. Coverage for Subscriptions and Resource Groups is tracked under the [Azure Resource Manager](./api-coverage/resource-manager) page.

:::

## Used ports
The ports used by Topaz can be divided into two groups:
* common port for Azure Resource Manager operations
* service-specific port for data plane

You can find which service uses which port below:
Service Name|Port|Protocol
------------|----|--------
Resource Manager|8899, 443|HTTPS
Table Storage|8890|HTTP
Blob Storage|8891|HTTP
Azure Key Vault|8898, 443|HTTPS
Azure Event Hub|8897|HTTPS
Azure Event Hub (AMQP)|8888|AMQP
Azure Service Bus|8887, 8899|HTTPS
Azure Service Bus (AMQP)|8889, 5671|AMQP, AMQP/TLS
Container Registry (control plane)|8899|HTTPS
Container Registry (data plane)|8892|HTTPS

For HTTPS endpoints, if you're running Topaz as a standalone application, you need to install and trust the certificates provided along with the main package.

:::warning[Admin privileges required for port 443]

Port 443 is a privileged port on Linux and macOS. If you are running Topaz as a **standalone executable** (not inside a container) and you need Azure CLI integration with Key Vault, you must start Topaz with `sudo` (or equivalent elevated privileges) so it can bind to port 443.

This requirement does **not** apply when running Topaz as a Docker container — Docker handles the privileged port mapping automatically.

If you are only using the Azure SDK (not the Azure CLI), you can point your SDK client directly at port 8898 and skip port 443 entirely.

:::
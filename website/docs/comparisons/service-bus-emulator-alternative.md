---
sidebar_position: 2
description: Looking for an Azure Service Bus Emulator alternative? Topaz provides a full ARM control plane, Azure CLI and Terraform support, and co-locates Service Bus with Key Vault, Event Hubs, and Storage in a single binary.
keywords: [azure service bus emulator alternative, service bus emulator replacement, local service bus emulator, topaz vs service bus emulator, azure service bus local development, service bus terraform, az servicebus cli local]
---

# Azure Service Bus Emulator alternative

If you're using the Azure Service Bus Emulator today, Topaz is a compatible alternative that covers Service Bus messaging — and adds the ARM control plane that makes the Azure CLI and Terraform work against it without any special emulator configuration.

## What is the Azure Service Bus Emulator?

The [Azure Service Bus Emulator](https://learn.microsoft.com/en-us/azure/service-bus-messaging/overview-emulator) is Microsoft's containerized local development environment for Service Bus. It implements the AMQP 1.0 messaging protocol and exposes a proprietary HTTP management API for creating queues, topics, and subscriptions.

The emulator ships as a Docker image and requires a companion SQL Server Linux container (Azure SQL Edge) as a storage backend. It is intended solely for development and testing — Microsoft provides no official support and distributes it under a proprietary EULA, separate from the MIT-licensed installer scripts.

Topaz is written in .NET 10 and ships as a single self-contained binary or Docker image. It emulates Service Bus alongside the broader Azure platform — Key Vault, Event Hubs, Storage, Container Registry, Managed Identity, and more — all in one process, all under the Apache 2.0 license.

## Architecture and setup

The most immediate practical difference is what you need to run each tool.

The Azure Service Bus Emulator requires **two Docker containers**: the emulator itself and an Azure SQL Edge container it depends on for storage. The minimum hardware requirement is 2 GB of RAM and 5 GB of disk space for the pair. Initial entities are declared in a `config.json` file that must be provided before the emulator starts — changes are not applied on the fly and require a container restart. The namespace name in the configuration file is fixed and cannot be renamed at runtime.

The emulator's connection string includes a proprietary flag:

```
Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

For management API calls, the port must be appended explicitly:

```
Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

Topaz runs as a single process with no external dependencies. Service Bus namespaces are created through the standard ARM API — the same `az servicebus namespace create` or `azurerm_servicebus_namespace` resource block you use in real Azure. Connection strings follow the real Azure format:

```
Endpoint=sb://my-namespace.servicebus.topaz.local.dev;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...;
```

There is no restart cycle and no static configuration file.

## Feature comparison

| Feature | Topaz | Azure SB Emulator |
|---|---|---|
| **Queues** (send, receive, peek) | ✅ | ✅ |
| **Topics and subscriptions** | ✅ | ✅ |
| **AMQP 1.0** | ✅ | ✅ |
| **AMQP/TLS** | ✅ | ✅ |
| **AMQP WebSockets** | ❌ | ❌ |
| **JMS protocol** | ❌ | ❌ |
| **Dead letter queues** | ❌ (planned) | ✅ |
| **Message sessions** | ❌ (planned) | ✅ |
| **Topic filters and rules** (correlation + SQL) | ❌ (planned) | ✅ |
| **Partitioned entities** | ❌ | ❌ |
| **Entity definitions persist across restarts** | ✅ | ❌ |
| **In-flight messages persist across restarts** | ❌ | ❌ |
| **Multiple namespaces** | ✅ (unlimited) | ❌ (max 1) |
| **ARM control plane** | ✅ | ❌ |
| **Azure CLI (`az servicebus`)** | ✅ | ❌ |
| **Terraform (`azurerm` provider)** | ✅ (namespace, queue, topic, subscription) | ❌ |
| **`ServiceBusAdministrationClient`** (.NET) | ✅ | ✅ |
| **`ServiceBusAdministrationClient`** (other languages) | ✅ | ⚠️ Non-TLS workarounds required |
| **Entra ID integration** | ✅ | ❌ |
| **License** | Apache 2.0 | Proprietary EULA |
| **External dependencies** | None | SQL Server Linux |
| **Co-located with other Azure services** | ✅ | ❌ (standalone) |

## ARM control plane

This is where the two tools diverge most significantly for teams that use infrastructure-as-code or the Azure CLI.

The Azure Service Bus Emulator does not implement the Azure Resource Manager API. It exposes a proprietary HTTP management API on port 5300 that the .NET `ServiceBusAdministrationClient` can target. This is useful for programmatic entity management within a .NET application, but it is not the ARM REST API. Consequently:

- `az servicebus namespace create` cannot target the emulator — the command calls `management.azure.com`, which the emulator does not implement
- Terraform's `azurerm_servicebus_namespace`, `azurerm_servicebus_queue`, `azurerm_servicebus_topic`, and `azurerm_servicebus_subscription` resources target the ARM API and cannot be applied against the emulator

Topaz implements the ARM control plane for Service Bus. The same `az servicebus` commands and the same Terraform resource blocks that work in real Azure work locally against Topaz without modification:

```bash
az servicebus namespace create \
  --name my-namespace \
  --resource-group my-rg \
  --location westeurope \
  --endpoint https://topaz.local.dev:8899

az servicebus queue create \
  --name orders \
  --namespace-name my-namespace \
  --resource-group my-rg \
  --endpoint https://topaz.local.dev:8899
```

```hcl
resource "azurerm_servicebus_namespace" "example" {
  name                = "my-namespace"
  location            = azurerm_resource_group.example.location
  resource_group_name = azurerm_resource_group.example.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "orders" {
  name         = "orders"
  namespace_id = azurerm_servicebus_namespace.example.id
}
```

For teams that provision infrastructure with Terraform or validate CLI-driven workflows locally, the absence of an ARM control plane in the emulator is a hard blocker. Topaz removes that blocker.

## Entity management

The Azure Service Bus Emulator configures initial entities via a `config.json` file provided at container start. The emulator documentation is explicit: *"You must supply any changes in JSON configuration before you run the emulator. Changes aren't honored on the fly. For changes to take effect, you must restart the container."* Additionally, if you use the `ServiceBusAdministrationClient` to create entities at runtime, those changes are lost the next time the container starts — the config file takes precedence and overwrites any runtime state on initialization.

The emulator supports exactly one namespace per container instance. The namespace name is fixed by the configuration and cannot be changed.

Topaz manages entities dynamically through the ARM API. Namespaces, queues, topics, and subscriptions can be created, updated, and deleted at runtime without any restart. Entity definitions are written to disk by the resource provider and survive process restarts — only in-flight AMQP messages are not persisted. There is no limit on the number of namespaces.

## SDK compatibility

The emulator's management API is natively supported only in .NET. The emulator's own documentation states: *"SDKs in other languages do not honor non-TLS connections or custom ports in the connection string. As a result, they do not natively support the Emulator's management APIs."* Python, Java, JavaScript, and Go SDKs require manual workarounds to reach the emulator's management endpoint.

Topaz uses the standard ARM REST API for entity management, so any language with an Azure SDK — Python, Java, JavaScript, Go, C# — can create and manage Service Bus resources through the standard resource management libraries without workarounds.

AMQP messaging (send and receive) works across all languages in both tools.

## Missing features in Topaz

For applications that rely on advanced messaging features, the current gaps in Topaz's Service Bus implementation are worth reviewing before switching:

- **Dead letter queues**: not yet implemented
- **Message sessions**: not yet implemented
- **Topic filters and rules** (correlation filters, SQL filters): not yet implemented
- **Authorization rules and SAS keys per entity**: not yet implemented

These features are planned for v1.7. The [roadmap](/roadmap) tracks the current status. If your application depends on any of these, the Azure Service Bus Emulator may be the better choice until they are available.

## Beyond Service Bus

The Azure Service Bus Emulator is scoped entirely to Service Bus. Topaz emulates the broader Azure platform in a single process:

| Service | Topaz | Azure SB Emulator |
|---|---|---|
| Service Bus (AMQP + HTTPS) | ✅ | ✅ |
| Event Hubs (AMQP + HTTPS) | ✅ | ❌ |
| Key Vault (secrets, keys, certificates) | ✅ | ❌ |
| Azure Storage (Blob, Table, Queue) | ✅ | ❌ |
| Container Registry (push, pull, tags) | ✅ | ❌ |
| Managed Identity | ✅ | ❌ |
| Entra ID (local token issuance) | ✅ | ❌ |
| RBAC (role assignments) | ✅ | ❌ |
| ARM control plane (resource groups, subscriptions) | ✅ | ❌ |
| ARM template / Bicep deployments | ✅ | ❌ |
| Terraform `azurerm` provider target | ✅ | ❌ |
| Azure CLI (`az servicebus`, `az keyvault`, …) | ✅ | ❌ |
| MCP server for AI tooling | ✅ | ❌ |

If your application uses Service Bus alongside any other Azure service, running them all in a single Topaz process is simpler than maintaining separate containers for each emulator.

## When to keep the emulator

The Azure Service Bus Emulator is the right choice if:

- Your application uses dead letter queues, message sessions, or topic filters and rules
- You are working exclusively in .NET and do not need ARM-level tooling
- You only need a single namespace and static entity configuration is sufficient
- You have existing CI/CD pipelines built around the emulator's Docker setup and the migration cost is not worth it

## When to switch to Topaz

Topaz is the right choice if:

- You need `az servicebus` commands or Terraform `azurerm_servicebus_*` resources to work locally
- Your application uses multiple Service Bus namespaces
- Your team uses Python, Java, JavaScript, or Go for management operations
- You are already using Topaz for other services and want a single process instead of multiple containers
- You need entity definitions (namespaces, queues, topics, subscriptions) to survive process restarts
- You want Apache 2.0 open-source software with no proprietary EULA restrictions

## Migrating from the emulator

Topaz implements the AMQP 1.0 messaging protocol. For send and receive operations, pointing your existing SDK clients at Topaz's AMQP endpoint is the main change — remove `UseDevelopmentEmulator=true` from the connection string and update the endpoint hostname and port.

For entity management, replace config.json-based setup with ARM API calls — either through `az servicebus` or Terraform. Topaz does not have a `config.json` equivalent; all entities are created through the standard ARM control plane.

Before migrating, verify that your application does not rely on dead letter queues, message sessions, or topic filters and rules — these are planned for v1.7 and not yet available. See the [roadmap](/roadmap) for the full list.

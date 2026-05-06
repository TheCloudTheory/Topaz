---
sidebar_position: 6
description: Use the Topaz MCP server to let AI assistants like GitHub Copilot manage your local Azure emulator with natural language — start, stop, and provision emulated Azure resources without manual CLI commands.
keywords: [topaz mcp server, mcp azure emulator, ai azure local, github copilot azure, model context protocol azure]
---

# MCP Server

Topaz ships a [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that lets AI assistants — such as GitHub Copilot in VS Code — start, stop, and manage the local emulator on your behalf. Instead of running CLI commands manually, you can describe what you need in natural language and let the assistant handle the infrastructure setup.

## How it works

The MCP server runs as a `stdio` process (spawned by your editor or AI tool) and exposes two kinds of capabilities:

- **Tools** — individual operations the assistant can call (create a resource group, fetch connection strings, check emulator health).
- **Prompts** — pre-defined multi-step recipes that tell the assistant which tools to call, in which order, and with which parameters to set up a complete scenario in one go.

The server uses the Testcontainers library to pull and manage the Topaz container, so **Docker must be running** on your machine.

## Available tools

### Setup tools

| Tool | Description |
|---|---|
| `RunTopazAsContainer` | Pulls and starts the Topaz emulator as a local Docker container, binding all common service ports |
| `StopTopazContainer` | Gracefully stops and removes the container that was started by `RunTopazAsContainer` |

`RunTopazAsContainer` accepts two optional parameters:

| Parameter | Default | Description |
|---|---|---|
| `logLevel` | `Information` | Emulator log verbosity (`Debug`, `Information`, `Warning`, `Error`) |
| `version` | latest alpha | Docker image tag to use (e.g. `v1.0.299-alpha`) |

The following ports are bound automatically when the container starts:

| Port | Service |
|---|---|
| 8899 | ARM / Resource Manager |
| 8898 | Key Vault |
| 8897 | Event Hub (HTTP) |
| 8891 | Blob Storage |
| 8890 | Table Storage |

### Subscription tools

| Tool | Description |
|---|---|
| `CreateSubscription` | Creates a subscription inside a running Topaz instance |
| `ListSubscriptions` | Returns all subscriptions currently registered in Topaz |

Both tools accept an `objectId` parameter — the Entra ID object ID of the acting user. Pass an empty GUID (`00000000-0000-0000-0000-000000000000`) to act as a superadmin with no permission restrictions.

### Diagnostics tools

| Tool | Description |
|---|---|
| `GetTopazStatus` | Calls the Topaz health-check endpoint and probes all known service ports. Returns the running version, overall status, working directory, and which services are up |

`GetTopazStatus` takes no parameters. It probes the following ports and reports whether each service is reachable:

| Port | Service |
|---|---|
| 8899 | Resource Manager |
| 8898 | Key Vault |
| 8891 | Blob Storage |
| 8893 | Queue Storage |
| 8890 | Table Storage |
| 8894 | File Storage |
| 8892 | Container Registry |
| 8897 | Event Hub (HTTP) |
| 8888 | Event Hub (AMQP) |
| 8889 | Service Bus (AMQP) |
| 8887 | Service Bus (Extra) |

This tool is useful for debugging a setup that fails partway through — ask the assistant to check status before investigating further.

### Resource tools

#### Provisioning

| Tool | Description |
|---|---|
| `CreateResourceGroup` | Creates a resource group in the given subscription |
| `CreateKeyVault` | Creates a Key Vault and optionally seeds it with an initial secret |
| `CreateStorageAccount` | Creates a Storage Account and returns its connection strings and service URIs |
| `CreateBlobContainer` | Creates a Blob container inside an existing Storage Account |
| `CreateStorageQueue` | Creates a Storage Queue inside an existing Storage Account |
| `CreateStorageTable` | Creates a Storage Table inside an existing Storage Account |
| `CreateServiceBusNamespace` | Creates a Service Bus namespace and returns its connection strings |
| `CreateServiceBusQueue` | Creates a queue inside an existing Service Bus namespace |
| `CreateServiceBusTopic` | Creates a topic inside an existing Service Bus namespace |
| `CreateServiceBusSubscription` | Creates a subscription on an existing Service Bus topic |
| `CreateEventHubNamespace` | Creates an Event Hub namespace and returns its connection string |
| `CreateEventHub` | Creates an Event Hub inside an existing namespace |
| `CreateContainerRegistry` | Creates a Container Registry and returns its login server and admin credentials |

All provisioning tools share these common parameters:

| Parameter | Description |
|---|---|
| `subscriptionId` | ID of the subscription to target |
| `objectId` | Entra ID object ID of the acting user. Pass an empty GUID (`00000000-0000-0000-0000-000000000000`) for superadmin access |
| `location` | Azure location string (e.g. `westeurope`, `eastus`) |

`CreateKeyVault` also accepts two optional parameters to seed an initial secret:

| Parameter | Description |
|---|---|
| `secretName` | Name of the secret to create |
| `secretValue` | Value of the secret (required when `secretName` is provided) |

`CreateServiceBusQueue` and `CreateServiceBusSubscription` each accept one optional parameter:

| Parameter | Default | Description |
|---|---|---|
| `maxDeliveryCount` | `10` | Maximum delivery attempts before a message is dead-lettered |

`CreateEventHub` accepts two optional parameters:

| Parameter | Default | Description |
|---|---|---|
| `partitionCount` | `4` | Number of partitions (1–32) |
| `messageRetentionInDays` | `1` | Retention period in days (1–7) |

`CreateContainerRegistry` accepts two optional parameters:

| Parameter | Default | Description |
|---|---|---|
| `sku` | `Basic` | Registry SKU: `Basic`, `Standard`, or `Premium` |
| `adminUserEnabled` | `true` | When `true`, admin credentials are returned alongside the login server |

#### Delete

| Tool | Description |
|---|---|
| `DeleteResourceGroup` | Deletes a resource group and all resources it contains |

#### Query

| Tool | Description |
|---|---|
| `GetConnectionStrings` | Queries all provisioned resources in a subscription and returns ready-to-use connection strings and URIs |

`GetConnectionStrings` accepts the following parameters:

| Parameter | Description |
|---|---|
| `subscriptionId` | ID of the subscription to query |
| `objectId` | Entra ID object ID of the acting user. Pass an empty GUID (`00000000-0000-0000-0000-000000000000`) for superadmin access |

The tool scans every resource group in the subscription and returns connection information for the following resource types:

| Resource type | Returned fields |
|---|---|
| Storage accounts | `connectionString`, `blobServiceUri`, `queueServiceUri`, `tableServiceUri` |
| Service Bus namespaces | `connectionString`, `connectionStringWithTls` |
| Key Vaults | `vaultUri` |
| Event Hub namespaces | `connectionString` |
| Container Registries | `loginServer` |

## Available prompts

Prompts are pre-defined conversation starters that the MCP server exposes alongside tools. When you invoke a prompt, the server returns a ready-made instruction message that tells the AI assistant exactly which tools to call, in which order, and with which parameters — so you don't have to describe the sequence yourself.

**Prompts vs. tools at a glance:**

| | Tools | Prompts |
|---|---|---|
| What they are | Individual operations the AI can call | Multi-step recipes the AI follows |
| When to use | When you need one specific action | When you want to set up a complete scenario |
| How to invoke | AI decides which tool fits your request | You explicitly select the prompt by name |

In VS Code with GitHub Copilot, start a prompt by typing its name in chat (e.g. `bootstrap-topaz`) or by asking Copilot to "use the bootstrap-topaz prompt". The assistant fills in the instructions, asks for any required parameters, and then executes the full tool sequence.

---

### Environment prompts

#### `bootstrap-topaz`

First-time setup. Starts the Topaz container, registers a subscription, creates an initial resource group, and confirms the emulator is healthy. This is the entry point — run it before any other provisioning prompt.

**Tool sequence:** `RunTopazAsContainer` → `CreateSubscription` → `CreateResourceGroup` → `GetTopazStatus`

| Parameter | Required | Default | Description |
|---|---|---|---|
| `subscriptionId` | ✅ | — | Subscription ID to create (e.g. `10000000-0000-0000-0000-000000000001`) |
| `subscriptionName` | ✅ | — | Human-readable subscription name |
| `resourceGroupName` | ✅ | — | Name of the initial resource group |
| `location` | ✅ | — | Azure location (e.g. `westeurope`) |
| `objectId` | ✅ | — | Entra ID object ID of the acting user. Use `00000000-0000-0000-0000-000000000000` for superadmin |
| `version` | — | `v1.2.6-beta` | Topaz Docker image tag to pull |

---

#### `inspect-environment`

Audits the running emulator in one pass: checks health, lists subscriptions, and returns connection strings for every provisioned resource. Use this when you need a complete picture of the current state or when debugging a broken setup.

**Tool sequence:** `GetTopazStatus` → `ListSubscriptions` → `GetConnectionStrings`

| Parameter | Required | Description |
|---|---|---|
| `subscriptionId` | ✅ | Subscription ID to inspect |
| `objectId` | ✅ | Entra ID object ID of the acting user |

The result is a structured report with three sections: emulator status, subscriptions list, and a resource inventory grouped by type with ready-to-use connection strings.

---

#### `teardown-environment`

Cleans up a session by deleting a resource group and all the resources it contains. Optionally stops the Topaz container. Use this at the end of a development or testing session.

**Tool sequence:** `DeleteResourceGroup` → (optional) `StopTopazContainer`

| Parameter | Required | Default | Description |
|---|---|---|---|
| `subscriptionId` | ✅ | — | Subscription ID containing the resource group |
| `resourceGroupName` | ✅ | — | Resource group to delete |
| `objectId` | ✅ | — | Entra ID object ID of the acting user |
| `stopContainer` | — | `false` | When `true`, also stops the Topaz container after deletion |

---

#### `setup-multi-tenant-fixtures`

Provisions isolated per-tenant resources following a naming convention — useful for testing tenant isolation or seeding fixtures for multi-tenant integration tests. For each tenant name in the list, the AI creates a dedicated subscription, resource group, storage account, and Key Vault.

**Tool sequence per tenant:** `CreateSubscription` → `CreateResourceGroup` → `CreateStorageAccount` → `CreateKeyVault`

| Parameter | Required | Description |
|---|---|---|
| `tenantNames` | ✅ | Comma-separated tenant names (e.g. `acme,globex,initech`) |
| `namingPrefix` | ✅ | Prefix applied to all resource names (e.g. `dev` → `dev-acme-rg`, `devacmestorage`) |
| `location` | ✅ | Azure location for all resources |
| `objectId` | ✅ | Entra ID object ID of the acting user |

---

### Application stack prompts

#### `setup-web-app-backend`

Provisions a typical web-application backend: a Storage Account with a Blob container for files or static assets, and a Key Vault that can be seeded with a database connection string. Returns all endpoints at the end.

**Tool sequence:** `CreateStorageAccount` → `CreateBlobContainer` → `CreateKeyVault` → `GetConnectionStrings`

| Parameter | Required | Description |
|---|---|---|
| `subscriptionId` | ✅ | |
| `resourceGroupName` | ✅ | |
| `location` | ✅ | |
| `storageAccountName` | ✅ | Storage account name (lowercase, 3–24 chars) |
| `containerName` | ✅ | Blob container name for uploads or assets |
| `keyVaultName` | ✅ | |
| `objectId` | ✅ | |
| `secretName` | — | Name of an initial Key Vault secret (e.g. `db-connection-string`) |
| `secretValue` | — | Value for that secret. Required when `secretName` is provided |

---

#### `setup-functions-local-dev`

Mirrors the minimum Azure Functions local-dev dependency set: a Storage Account (required by the Functions runtime for `AzureWebJobsStorage`), a Service Bus queue used as a trigger, and a Key Vault with the storage connection string already stored as a secret.

**Tool sequence:** `CreateStorageAccount` → `CreateServiceBusNamespace` → `CreateServiceBusQueue` → `CreateKeyVault`

| Parameter | Required | Description |
|---|---|---|
| `subscriptionId` | ✅ | |
| `resourceGroupName` | ✅ | |
| `location` | ✅ | |
| `storageAccountName` | ✅ | Used as `AzureWebJobsStorage` |
| `serviceBusNamespaceName` | ✅ | |
| `triggerQueueName` | ✅ | Queue that triggers the function |
| `keyVaultName` | ✅ | Vault where `AzureWebJobsStorage` is stored as a secret |
| `objectId` | ✅ | |

After running, the prompt returns a ready-to-paste `local.settings.json` snippet.

---

#### `setup-event-driven-microservice`

Provisions the canonical command-event split: a Service Bus namespace with a command queue and an event topic (with a subscription for fan-out), plus a Key Vault with the connection string. Models the separation of write-side commands from read-side events.

**Tool sequence:** `CreateServiceBusNamespace` → `CreateServiceBusQueue` → `CreateServiceBusTopic` → `CreateServiceBusSubscription` → `CreateKeyVault`

| Parameter | Required | Description |
|---|---|---|
| `subscriptionId` | ✅ | |
| `resourceGroupName` | ✅ | |
| `location` | ✅ | |
| `namespaceName` | ✅ | Service Bus namespace |
| `commandQueueName` | ✅ | Queue for incoming commands |
| `eventTopicName` | ✅ | Topic for outgoing domain events |
| `subscriptionName` | ✅ | Subscription on the event topic |
| `keyVaultName` | ✅ | |
| `objectId` | ✅ | |

---

#### `setup-document-pipeline`

Provisions a multi-stage document-processing pipeline: a Storage Account with separate input and output Blob containers, a Service Bus topic (with a subscription) for routing notifications between stages, and a Key Vault for API keys.

**Tool sequence:** `CreateStorageAccount` → `CreateBlobContainer` (×2) → `CreateServiceBusNamespace` → `CreateServiceBusTopic` → `CreateServiceBusSubscription` → `CreateKeyVault`

| Parameter | Required | Description |
|---|---|---|
| `subscriptionId` | ✅ | |
| `resourceGroupName` | ✅ | |
| `location` | ✅ | |
| `storageAccountName` | ✅ | |
| `inputContainerName` | ✅ | Container for incoming documents |
| `outputContainerName` | ✅ | Container for processed output |
| `serviceBusNamespaceName` | ✅ | |
| `topicName` | ✅ | Topic for processing notifications |
| `subscriptionName` | ✅ | Subscription on the topic |
| `keyVaultName` | ✅ | |
| `objectId` | ✅ | |

---

#### `setup-event-ingestion`

Provisions an event-ingestion stack: a Storage Account with a capture container, an Event Hub namespace with a named hub, and a Key Vault seeded with the Event Hub connection string. Use this to test producers and consumers locally before pointing them at Azure.

**Tool sequence:** `CreateStorageAccount` → `CreateBlobContainer` → `CreateEventHubNamespace` → `CreateEventHub` → `CreateKeyVault` → `GetConnectionStrings`

| Parameter | Required | Default | Description |
|---|---|---|---|
| `subscriptionId` | ✅ | — | |
| `resourceGroupName` | ✅ | — | |
| `location` | ✅ | — | |
| `namespaceName` | ✅ | — | Event Hub namespace |
| `eventHubName` | ✅ | — | Hub name within the namespace |
| `storageAccountName` | ✅ | — | Used for event capture |
| `captureContainerName` | ✅ | — | Blob container for captured events |
| `keyVaultName` | ✅ | — | |
| `objectId` | ✅ | — | |
| `partitionCount` | — | `4` | Number of partitions (1–32) |

---

#### `setup-container-registry-stack`

Provisions a Container Registry with admin credentials, a backing Storage Account, and a Key Vault with the registry password stored as a secret. After setup the prompt produces a ready-to-run `docker login` command for the emulated registry.

**Tool sequence:** `CreateContainerRegistry` → `CreateStorageAccount` → `CreateKeyVault`

| Parameter | Required | Default | Description |
|---|---|---|---|
| `subscriptionId` | ✅ | — | |
| `resourceGroupName` | ✅ | — | |
| `location` | ✅ | — | |
| `registryName` | ✅ | — | Registry name (5–50 alphanumeric chars) |
| `storageAccountName` | ✅ | — | |
| `keyVaultName` | ✅ | — | |
| `objectId` | ✅ | — | |
| `sku` | — | `Basic` | Registry SKU: `Basic`, `Standard`, or `Premium` |

---

## Configuration

The MCP server is distributed as a Docker image (`thecloudtheory/topaz-mcp`). Add it to your editor's MCP configuration to make it available to the AI assistant.

### VS Code (GitHub Copilot)

Create or update `.vscode/mcp.json` in your workspace:

```json
{
  "servers": {
    "Topaz": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run",
        "--rm",
        "-i",
        "--network", "host",
        "thecloudtheory/topaz-mcp:<version>"
      ]
    }
  }
}
```

Replace `<version>` with the image tag matching your Topaz release (e.g. `v1.0.299-alpha`). All available tags are listed on the [topaz-mcp Docker Hub page](https://hub.docker.com/r/thecloudtheory/topaz-mcp/tags). Tags follow the same versioning scheme as the `topaz-host` image.

:::tip[`--network host`]

The `--network host` flag lets the MCP container reach the Topaz emulator container on `localhost`. Without it, the two containers are isolated in different Docker networks and subscription/resource operations will fail to connect.

:::

After saving the file, VS Code will prompt you to start the server. Once running, it appears in the MCP Servers panel and GitHub Copilot can call its tools.

### Other editors / AI tools

Any MCP-compatible client can use the server. The command to invoke it is:

```bash
docker run --rm -i --network host thecloudtheory/topaz-mcp:<version>
```

Refer to your tool's documentation for how to register a `stdio`-based MCP server.

## Example workflow

With the MCP server configured in VS Code, you can ask GitHub Copilot to set up your full local environment in a single conversation:

> "Start Topaz locally using the latest beta tag, create a subscription called `dev-local`, add a resource group `rg-dev` in `westeurope`, then provision a storage account, a Service Bus namespace with a queue named `orders`, and a Key Vault with a secret `db-password`."

Copilot will:
1. Call `RunTopazAsContainer` to pull and start the emulator
2. Call `CreateSubscription` to provision the subscription
3. Call `CreateResourceGroup` to create `rg-dev`
4. Call `CreateStorageAccount`, `CreateServiceBusNamespace`, `CreateServiceBusQueue`, and `CreateKeyVault` in sequence

You can then continue using `az` commands or the Azure SDK against `localhost` as described in the [Azure CLI integration](./integrations/azure-cli-integration.md) guide.

Once you have provisioned resources, ask Copilot to retrieve all connection strings at once:

> "Give me the connection strings for everything in my `dev-local` subscription."

Copilot will call `GetConnectionStrings` and return a structured list of URIs and connection strings ready to paste into your application configuration.

If something isn't working as expected, ask the assistant to run a health check:

> "Check whether Topaz is running and which services are up."

Copilot will call `GetTopazStatus`, which hits the health endpoint and probes every service port, so you can immediately see which services are reachable without leaving your editor.

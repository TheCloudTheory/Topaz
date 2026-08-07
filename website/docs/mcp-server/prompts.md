---
sidebar_position: 4
description: Pre-defined multi-step recipes exposed by the Topaz MCP server for common local Azure development scenarios.
---

# Prompts

Prompts are pre-defined conversation starters that the MCP server exposes alongside tools. When you invoke a prompt, the server returns a ready-made instruction message that tells the AI assistant exactly which tools to call, in which order, and with which parameters — so you don't have to describe the sequence yourself.

**Prompts vs. tools at a glance:**

| | Tools | Prompts |
|---|---|---|
| What they are | Individual operations the AI can call | Multi-step recipes the AI follows |
| When to use | When you need one specific action | When you want to set up a complete scenario |
| How to invoke | AI decides which tool fits your request | You explicitly select the prompt by name |

In VS Code with GitHub Copilot, start a prompt by typing its name in chat (e.g. `bootstrap-topaz`) or by asking Copilot to "use the bootstrap-topaz prompt". The assistant fills in the instructions, asks for any required parameters, and then executes the full tool sequence.

---

## Environment prompts

### `bootstrap-topaz`

First-time setup. Starts the Topaz container, registers a subscription, creates an initial resource group, and confirms the emulator is healthy. This is the entry point — run it before any other provisioning prompt.

**Tool sequence:** `RunTopazAsContainer` → `CreateSubscription` → `CreateResourceGroup` → `GetTopazStatus`

| Parameter | Required | Default | Description |
|---|---|---|---|
| `subscriptionId` | ✅ | — | Subscription ID to create (e.g. `10000000-0000-0000-0000-000000000001`) |
| `subscriptionName` | ✅ | — | Human-readable subscription name |
| `resourceGroupName` | ✅ | — | Name of the initial resource group |
| `location` | ✅ | — | Azure location (e.g. `westeurope`) |
| `objectId` | ✅ | — | Entra ID object ID of the acting user. Use `00000000-0000-0000-0000-000000000000` for superadmin |
| `version` | — | latest stable | Topaz Docker image tag to pull |

---

### `inspect-environment`

Audits the running emulator in one pass: checks health, lists subscriptions, and returns connection strings for every provisioned resource. Use this when you need a complete picture of the current state or when debugging a broken setup.

**Tool sequence:** `GetTopazStatus` → `ListSubscriptions` → `GetConnectionStrings`

| Parameter | Required | Description |
|---|---|---|
| `subscriptionId` | ✅ | Subscription ID to inspect |
| `objectId` | ✅ | Entra ID object ID of the acting user |

The result is a structured report with three sections: emulator status, subscriptions list, and a resource inventory grouped by type with ready-to-use connection strings.

---

### `teardown-environment`

Cleans up a session by deleting a resource group and all the resources it contains. Optionally stops the Topaz container. Use this at the end of a development or testing session.

**Tool sequence:** `DeleteResourceGroup` → (optional) `StopTopazContainer`

| Parameter | Required | Default | Description |
|---|---|---|---|
| `subscriptionId` | ✅ | — | Subscription ID containing the resource group |
| `resourceGroupName` | ✅ | — | Resource group to delete |
| `objectId` | ✅ | — | Entra ID object ID of the acting user |
| `stopContainer` | — | `false` | When `true`, also stops the Topaz container after deletion |

---

### `setup-multi-tenant-fixtures`

Provisions isolated per-tenant resources following a naming convention — useful for testing tenant isolation or seeding fixtures for multi-tenant integration tests. For each tenant name in the list, the AI creates a dedicated subscription, resource group, storage account, and Key Vault.

**Tool sequence per tenant:** `CreateSubscription` → `CreateResourceGroup` → `CreateStorageAccount` → `CreateKeyVault`

| Parameter | Required | Description |
|---|---|---|
| `tenantNames` | ✅ | Comma-separated tenant names (e.g. `acme,globex,initech`) |
| `namingPrefix` | ✅ | Prefix applied to all resource names (e.g. `dev` → `dev-acme-rg`, `devacmestorage`) |
| `location` | ✅ | Azure location for all resources |
| `objectId` | ✅ | Entra ID object ID of the acting user |

---

## Application stack prompts

### `setup-web-app-backend`

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

### `setup-functions-local-dev`

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

### `setup-event-driven-microservice`

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

### `setup-document-pipeline`

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

### `setup-event-ingestion`

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

### `setup-container-registry-stack`

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

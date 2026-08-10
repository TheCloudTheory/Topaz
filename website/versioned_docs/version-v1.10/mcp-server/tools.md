---
sidebar_position: 3
description: Complete reference for all tools exposed by the Topaz MCP server.
---

# Tools

Tools are individual operations the AI assistant can call. The following tools are available in the Topaz MCP server.

## Common parameters

All provisioning tools share these common parameters:

| Parameter | Description |
|---|---|
| `subscriptionId` | ID of the subscription to target |
| `objectId` | Entra ID object ID of the acting user. Pass an empty GUID (`00000000-0000-0000-0000-000000000000`) for superadmin access |
| `location` | Azure location string (e.g. `westeurope`, `eastus`) |

## Setup tools

| Tool | Description |
|---|---|
| `RunTopazAsContainer` | Creates a shared `topaz-net` Docker network, starts a lightweight DNS resolver (`topaz-dns`) that handles all `*.topaz.local.dev` wildcard subdomains, then pulls and starts the Topaz emulator container at a fixed IP on that network |
| `ConnectMcpToTopazNetwork` | Returns a `docker network connect` command to attach an already-running MCP container to `topaz-net`. Use this when the MCP container was started before `RunTopazAsContainer` was called. Note: full wildcard subdomain DNS support requires `--dns 172.28.0.53` at container creation time; connecting a running container only restores base ARM connectivity |
| `StopTopazContainer` | Gracefully stops and removes the Topaz emulator container, the DNS resolver container, and the `topaz-net` Docker network |

`RunTopazAsContainer` accepts the following optional parameters:

| Parameter | Default | Description |
|---|---|---|
| `logLevel` | `Information` | Emulator log verbosity (`Debug`, `Information`, `Warning`, `Error`) |
| `version` | latest stable | Docker image tag to use (e.g. `v1.9.0`) |
| `platform` | `linux/amd64` | Docker platform: `linux/arm64` for Apple Silicon / ARM64 hosts, `linux/amd64` for Intel/AMD hosts |

The following ports are bound automatically when the container starts:

| Port | Service |
|---|---|
| 8899 | ARM / Resource Manager |
| 8898 | Key Vault |
| 8897 | Event Hub (HTTP) |
| 8896 | App Service (Kudu) |
| 8895 | Cosmos DB |
| 8893 | App Configuration |
| 8892 | Container Registry |
| 8891 | Storage (Blob, Queue, Table, File) |
| 8889 | Service Bus (AMQP) |
| 8888 | Event Hub (AMQP) |
| 8887 | Service Bus (Extra) |

## Subscription tools

| Tool | Description |
|---|---|
| `CreateSubscription` | Creates a subscription inside a running Topaz instance |
| `ListSubscriptions` | Returns all subscriptions currently registered in Topaz |

Both tools accept an `objectId` parameter — the Entra ID object ID of the acting user. Pass an empty GUID (`00000000-0000-0000-0000-000000000000`) to act as a superadmin with no permission restrictions.

## Diagnostics tools

| Tool | Description |
|---|---|
| `GetTopazStatus` | Calls the Topaz health-check endpoint and probes all known service ports. Returns the running version, overall status, working directory, and which services are up |

`GetTopazStatus` takes no parameters. It probes the following ports and reports whether each service is reachable:

| Port | Service |
|---|---|
| 8899 | Resource Manager |
| 8898 | Key Vault |
| 8897 | Event Hub (HTTP) |
| 8896 | App Service (Kudu) |
| 8895 | Cosmos DB |
| 8893 | App Configuration |
| 8892 | Container Registry |
| 8891 | Storage (Blob, Queue, Table, File) |
| 8889 | Service Bus (AMQP) |
| 8888 | Event Hub (AMQP) |
| 8887 | Service Bus (Extra) |

This tool is useful for debugging a setup that fails partway through — ask the assistant to check status before investigating further.

## Resource tools

### Tenant-scope deployments

| Tool | Description |
|---|---|
| `CreateOrUpdateTenantDeployment` | Creates or updates a tenant-scope ARM template deployment and polls until the orchestrator finishes |
| `GetTenantDeployment` | Returns a tenant-scope deployment by name |
| `DeleteTenantDeployment` | Deletes a tenant-scope deployment by name |

All three tools accept an `objectId` parameter. `CreateOrUpdateTenantDeployment` also requires `deploymentName`, `location`, and `templateJson` (ARM template as a JSON string). `GetTenantDeployment` and `DeleteTenantDeployment` require only `deploymentName` and `objectId`.

`CreateOrUpdateTenantDeployment` returns a `TenantDeploymentResult` with `Name`, `Id`, and `ProvisioningState` fields. It polls Topaz until the provisioning state leaves `Created` or `Running`, so the caller always receives a terminal state.

### Provisioning

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
| `CreateCosmosDbAccount` | Creates a Cosmos DB account (SQL API) and returns the account endpoint and connection string |
| `CreateCosmosDbDatabase` | Creates a SQL database inside an existing Cosmos DB account |
| `CreateCosmosDbContainer` | Creates a SQL container inside an existing Cosmos DB database |
| `CreateAppConfigurationStore` | Creates an App Configuration store and returns its endpoint URL and primary read-write connection string |
| `CreateApplicationInsights` | Creates an Application Insights component and returns its connection string and instrumentation key |
| `CreateLogAnalyticsWorkspace` | Creates a Log Analytics workspace |
| `CreateAppServicePlan` | Creates an App Service plan |
| `CreateAppServiceSite` | Creates an App Service site (web app) inside an existing App Service plan |
| `CreateRedisCache` | Creates a Redis Cache instance and returns its host name and connection string |
| `CreateSqlServer` | Creates a SQL Server instance and returns its fully qualified domain name |

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

`CreateCosmosDbDatabase` accepts one optional parameter:

| Parameter | Default | Description |
|---|---|---|
| `throughput` | (serverless) | Optional throughput in RU/s for the database |

`CreateCosmosDbContainer` requires `accountName`, `databaseName`, `containerName`, and `partitionKeyPath` (e.g. `/id`), plus one optional parameter:

| Parameter | Default | Description |
|---|---|---|
| `throughput` | (serverless) | Optional throughput in RU/s for the container |

### Delete

| Tool | Description |
|---|---|
| `DeleteResourceGroup` | Deletes a resource group and all resources it contains |

### Query

| Tool | Description |
|---|---|
| `GetConnectionStrings` | Queries all provisioned resources in a subscription and returns ready-to-use connection strings and URIs |

`GetConnectionStrings` scans every resource group in the subscription and returns connection information for the following resource types:

| Resource type | Returned fields |
|---|---|
| Storage accounts | `connectionString`, `blobServiceUri`, `queueServiceUri`, `tableServiceUri` |
| Service Bus namespaces | `connectionString`, `connectionStringWithTls` |
| Key Vaults | `vaultUri` |
| Event Hub namespaces | `connectionString` |
| Container Registries | `loginServer` |
| Cosmos DB accounts | `accountEndpoint`, `primaryConnectionString` |
| App Configuration stores | `endpoint`, `primaryReadWriteConnectionString` |
| Redis Cache instances | `hostName`, `connectionString` |

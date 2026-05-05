---
sidebar_position: 6
description: Use the Topaz MCP server to let AI assistants like GitHub Copilot manage your local Azure emulator with natural language — start, stop, and provision emulated Azure resources without manual CLI commands.
keywords: [topaz mcp server, mcp azure emulator, ai azure local, github copilot azure, model context protocol azure]
---

# MCP Server

Topaz ships a [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server that lets AI assistants — such as GitHub Copilot in VS Code — start, stop, and manage the local emulator on your behalf. Instead of running CLI commands manually, you can describe what you need in natural language and let the assistant handle the infrastructure setup.

## How it works

The MCP server runs as a `stdio` process (spawned by your editor or AI tool) and exposes a set of tools the assistant can call. The server itself uses the Testcontainers library to pull and manage the Topaz container, so **Docker must be running** on your machine.

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
| `CreateServiceBusNamespace` | Creates a Service Bus namespace and returns its connection strings |
| `CreateServiceBusQueue` | Creates a queue inside an existing Service Bus namespace |
| `CreateServiceBusTopic` | Creates a topic inside an existing Service Bus namespace |

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

`CreateServiceBusQueue` accepts one optional parameter:

| Parameter | Default | Description |
|---|---|---|
| `maxDeliveryCount` | `10` | Maximum delivery attempts before a message is dead-lettered |

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

Replace `<version>` with the image tag matching your Topaz release (e.g. `v1.0.299-alpha`). Tags follow the same versioning scheme as the `topaz-host` image.

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

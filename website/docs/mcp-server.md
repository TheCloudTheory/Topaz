---
sidebar_position: 5
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

Replace `<version>` with the image tag matching your Topaz release (e.g. `v1.0.299-alpha`). Tags follow the same versioning scheme as the `topaz-cli` image.

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

With the MCP server configured in VS Code, you can ask GitHub Copilot to set up your local environment:

> "Start Topaz locally using the latest alpha tag and create a subscription called `dev-local`."

Copilot will:
1. Call `RunTopazAsContainer` to pull and start the emulator
2. Call `CreateSubscription` to provision the subscription inside it

You can then continue using `az` commands or the Azure SDK against `localhost` as described in the [Azure CLI integration](./azure-cli-integration.md) guide.

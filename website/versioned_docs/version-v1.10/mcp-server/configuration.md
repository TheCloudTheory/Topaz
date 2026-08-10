---
sidebar_position: 2
description: Configure the Topaz MCP server in VS Code, GitHub Copilot, and other MCP-compatible editors.
---

# Configuration

The MCP server is distributed as a Docker image (`thecloudtheory/topaz-mcp`). Add it to your editor's MCP configuration to make it available to the AI assistant.

## Prerequisites

Before configuring the MCP server, create the shared Docker network once:

```bash
docker network create --subnet 172.28.0.0/16 topaz-net
```

This is a one-time step. The network persists across reboots until you remove it manually.

## VS Code (GitHub Copilot)

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
        "--network", "topaz-net",
        "--dns", "172.28.0.53",
        "thecloudtheory/topaz-mcp:<version>"
      ]
    }
  }
}
```

Replace `<version>` with the image tag matching your Topaz release (e.g. `v1.9.0`). All available tags are listed on the [topaz-mcp Docker Hub page](https://hub.docker.com/r/thecloudtheory/topaz-mcp/tags). Tags follow the same versioning scheme as the `topaz-host` image.

:::tip[Network and DNS setup]

The `--network topaz-net` flag places the MCP container on the same Docker network as the Topaz emulator. The `--dns 172.28.0.53` flag points DNS at the lightweight `topaz-dns` resolver (started automatically by `RunTopazAsContainer`) which resolves all `*.topaz.local.dev` wildcard subdomains — including Key Vault, Storage, Service Bus, and Event Hub data-plane hostnames — to the Topaz container. Both flags are required for full connectivity.

:::

After saving the file, VS Code will prompt you to start the server. Once running, it appears in the MCP Servers panel and GitHub Copilot can call its tools.

## Other editors / AI tools

Any MCP-compatible client can use the server. Create the shared network once (see above), then invoke the server with:

```bash
docker run --rm -i --network topaz-net --dns 172.28.0.53 thecloudtheory/topaz-mcp:<version>
```

Refer to your tool's documentation for how to register a `stdio`-based MCP server.

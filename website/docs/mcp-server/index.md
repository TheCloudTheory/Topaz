---
sidebar_position: 1
sidebar_label: Overview
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

## Example workflow

With the MCP server configured in VS Code, you can ask GitHub Copilot to set up your full local environment in a single conversation:

> "Start Topaz locally using the latest beta tag, create a subscription called `dev-local`, add a resource group `rg-dev` in `westeurope`, then provision a storage account, a Service Bus namespace with a queue named `orders`, and a Key Vault with a secret `db-password`."

Copilot will:
1. Call `RunTopazAsContainer` to pull and start the emulator
2. Call `CreateSubscription` to provision the subscription
3. Call `CreateResourceGroup` to create `rg-dev`
4. Call `CreateStorageAccount`, `CreateServiceBusNamespace`, `CreateServiceBusQueue`, and `CreateKeyVault` in sequence

You can then continue using `az` commands or the Azure SDK against `localhost` as described in the [Azure CLI integration](../integrations/azure-cli-integration.md) guide.

Once you have provisioned resources, ask Copilot to retrieve all connection strings at once:

> "Give me the connection strings for everything in my `dev-local` subscription."

Copilot will call `GetConnectionStrings` and return a structured list of URIs and connection strings ready to paste into your application configuration.

If something isn't working as expected, ask the assistant to run a health check:

> "Check whether Topaz is running and which services are up."

Copilot will call `GetTopazStatus`, which hits the health endpoint and probes every service port, so you can immediately see which services are reachable without leaving your editor.

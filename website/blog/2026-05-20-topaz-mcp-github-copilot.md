---
slug: topaz-mcp-github-copilot
title: "Using Topaz with GitHub Copilot via MCP Server"
description: How to connect the Topaz MCP server to GitHub Copilot so your AI assistant can provision local Azure infrastructure, Key Vault, Storage, Service Bus, and Event Hubs, directly from a chat prompt. Covers the Docker network and DNS setup needed for container-to-container connectivity.
keywords: [topaz mcp server, github copilot azure, mcp azure emulator, local azure ai assistant, model context protocol azure, github copilot local dev, azure mcp server]
authors: kamilmrzyglod
tags: [general, ai]
---

I wanted GitHub Copilot to do more than generate a bash script that calls `az group create`. I wanted to describe the local Azure stack in natural language and have the assistant provision it while I worked on the application code. Instead of tab-switching to a terminal, remembering the right parameter names, and sequencing five CLI commands in the right order, I wanted the infrastructure to be created directly from the chat, with connection strings returned in a form I could paste into the application configuration.

Topaz ships a [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server that makes this possible against the local emulator. The interesting part was not the tool call itself, but getting the container networking, DNS, and certificate setup right so those tool calls returned endpoints that were actually usable. This post covers how the MCP server works, why the Docker networking setup is non-trivial, and what the full setup looks like end to end.

{/* truncate */}

:::tip[Try it now]
Configure the Topaz MCP server in VS Code with three steps: create the Docker network, add one entry to `.vscode/mcp.json`, and start chatting.

```bash
docker network create --subnet 172.28.0.0/16 topaz-net
```

```json
{
  "servers": {
    "Topaz": {
      "type": "stdio",
      "command": "docker",
      "args": ["run", "--rm", "-i", "--network", "topaz-net", "--dns", "172.28.0.53", "thecloudtheory/topaz-mcp:latest"]
    }
  }
}
```

[MCP server docs →](https://topaz.thecloudtheory.com/docs/mcp-server) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

## What MCP actually does here

The Model Context Protocol is a standard that lets AI assistants call external tools and receive structured results, creating real side effects rather than generating commands for you to copy-paste and run manually. When Copilot calls a tool, the call goes to the MCP server process, the server executes it against a live Topaz instance, and the result comes back to the assistant. The assistant sees real data: the vault URI of the Key Vault it just created, the connection string for the Service Bus namespace, the login server for the Container Registry.

This matters because the assistant can chain those results. After creating a Storage Account, it can pass the connection string directly to the next tool call that creates a Key Vault secret, without you manually copying values between terminal windows.

The Topaz MCP server exposes two kinds of capabilities:

- **Tools**: individual operations such as create a resource group, provision a Key Vault, fetch all connection strings in a subscription, or check emulator health.
- **Prompts**: pre-defined multi-step recipes that wire tools together into complete scenarios, such as bootstrapping a full dev environment, setting up a Functions-ready stack, or provisioning a document processing pipeline.

## The Docker connectivity problem

When you run the MCP server as a Docker container, which is the recommended way to distribute it so clients do not need the .NET runtime, it needs to reach the Topaz host over the network. The Topaz host also runs as a Docker container. Two containers running on the same machine are not on the same network by default; they cannot reach each other by hostname.

The first instinct is `--network host`. The Docker documentation describes this as "the container shares the host's network namespace". On Linux this is exactly what it means, the container sees `localhost`, all published ports, and everything else on the host's network stack. In practice, this works when Topaz runs directly on the host machine. But it breaks as soon as both Topaz and the MCP server are running as containers, because `localhost` inside the MCP container is the Linux VM's loopback, not a path to the Topaz container.

Even on Linux, where `--network host` does give the MCP container access to the host network stack, there is a second problem: wildcard subdomains. The Topaz MCP tools do not only call `topaz.local.dev:8899` for ARM operations. When the Key Vault tool calls the Key Vault data-plane, it calls `<vault-name>.vault.topaz.local.dev:8898`. When the Container Registry tool calls the registry endpoint, it calls `<registry-name>.cr.topaz.local.dev:8892`. These subdomains are dynamic, they depend on resource names the user provides at runtime. `--network host` gives network access but does nothing for DNS; `topaz.local.dev` might resolve if the host has it in `/etc/hosts`, but no host-machine `/etc/hosts` entry covers `my-vault.vault.topaz.local.dev`.

What actually worked was a shared Docker network plus a wildcard DNS resolver.

## The network and DNS setup

The `RunTopazAsContainer` tool returns a shell command that sets up the full environment in one step. The command does three things:

**1. Create a user-defined bridge network with a fixed subnet:**
```bash
docker network create --subnet 172.28.0.0/16 topaz-net
```

User-defined networks have Docker's built-in DNS, so containers can resolve each other by name. The fixed subnet allows assigning stable IP addresses, which the dnsmasq configuration depends on.

**2. Start a lightweight DNS resolver at a fixed IP:**
```bash
docker run -d --name topaz-dns \
  --network topaz-net --ip 172.28.0.53 \
  alpine sh -c "apk add -q --no-cache dnsmasq && \
    dnsmasq --no-daemon --no-resolv --server=8.8.8.8 \
            --address=/.topaz.local.dev/172.28.0.10"
```

The `--address=/.topaz.local.dev/172.28.0.10` directive tells dnsmasq to resolve every hostname that ends in `.topaz.local.dev`, at any depth, to `172.28.0.10`. That covers `topaz.local.dev` itself, `my-vault.vault.topaz.local.dev`, `stdev.blob.storage.topaz.local.dev`, any registry name, and any Service Bus namespace. The resolver is at `172.28.0.53` following DNS convention; all queries it does not know about forward to `8.8.8.8`.

**3. Start the Topaz host container at the fixed IP:**
```bash
docker run -d --name topaz.local.dev \
  --network topaz-net --ip 172.28.0.10 \
  -p 8899:8899 -p 8898:8898 ... \
  thecloudtheory/topaz-host:<version>
```

The Topaz container is named `topaz.local.dev`. On a user-defined network, Docker DNS resolves container names, so even without the dnsmasq sidecar, the base `topaz.local.dev` hostname would resolve by container name alone. The dnsmasq sidecar handles everything else.

The MCP container connects to the same network and points at the DNS sidecar:

```bash
docker run -i --network topaz-net --dns 172.28.0.53 thecloudtheory/topaz-mcp:<version>
```

The `--dns` flag is set at container creation time. It controls what Docker writes into the container's `/etc/resolv.conf` before any process starts. Once the MCP container is running, `topaz.local.dev` and all its subdomains resolve to `172.28.0.10`, which is the Topaz host.

## The certificate

Every Topaz endpoint is HTTPS. The Topaz host uses a self-signed certificate that covers `*.topaz.local.dev` and all its subdomain patterns. The MCP container image has this certificate pre-installed in the Ubuntu system CA store. It is baked in during the Docker image build the same way the [Compose example app](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Compose) handles it:

```dockerfile
FROM ubuntu:noble AS cert-builder
RUN apt-get update -qq && apt-get install -y --no-install-recommends ca-certificates
COPY certificate/topaz.crt /usr/local/share/ca-certificates/topaz.crt
RUN update-ca-certificates

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled AS final
COPY --from=cert-builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/ca-certificates.crt
```

The cert-builder stage runs `update-ca-certificates` (not available in the minimal chiseled final image) and the updated CA bundle is copied across. The MCP server process inherits the system CA store and trusts Topaz's certificate without any code-level bypass.

## Setting it up in VS Code

The one-time setup is:

```bash
# Create the shared Docker network once, it survives reboots until you remove it manually
docker network create --subnet 172.28.0.0/16 topaz-net
```

Then add the server to `.vscode/mcp.json`:

```json
{
  "servers": {
    "Topaz": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run", "--rm", "-i",
        "--network", "topaz-net",
        "--dns", "172.28.0.53",
        "thecloudtheory/topaz-mcp:<version>"
      ]
    }
  }
}
```

After saving, VS Code prompts you to start the server. It appears in the MCP Servers panel, and GitHub Copilot picks up the tools automatically.

The first time you use the MCP server, ask Copilot to start Topaz:

> "Start Topaz using the latest beta image for Apple Silicon."

Copilot calls `RunTopazAsContainer` with `platform=linux/arm64`, gets back a shell command, and tells you to run it. After that, every provisioning tool has a live Topaz instance to talk to. Depending on the LLM you're using you can just skip the platform architecture part:

> "Start Topaz using the latest beta image."

Most of the models will be able to infer the architecture automatically and pass the correct `platform` parameter.

## What a real session looks like

Once the emulator is running, provisioning a complete local dev environment is a single chat message:

> "Create a subscription called dev-local with ID `10000000-0000-0000-0000-000000000001`, a resource group `rg-dev` in `westeurope`, a storage account `stdevlocal001`, a Service Bus namespace `sbdevlocal` with a queue called `orders`, and a Key Vault `kv-dev` with a secret `connection-string` set to the Service Bus connection string. Use superadmin access."

Copilot issues seven tool calls in sequence:

```
CreateSubscription(subscriptionId=10000000-..., subscriptionName=dev-local, objectId=00000000-...)
CreateResourceGroup(subscriptionId=10000000-..., resourceGroupName=rg-dev, location=westeurope, ...)
CreateStorageAccount(...)
CreateServiceBusNamespace(...)
CreateServiceBusQueue(namespaceName=sbdevlocal, queueName=orders, ...)
CreateKeyVault(...)
  → vaultUri = https://kv-dev.vault.topaz.local.dev:8898
  → seededSecret = connection-string
GetConnectionStrings(subscriptionId=10000000-..., ...)
```

The final `GetConnectionStrings` call returns a structured list:

```
Storage account stdevlocal001:
  connectionString: DefaultEndpointsProtocol=https;AccountName=stdevlocal001;...
  blobServiceUri:   https://stdevlocal001.blob.storage.topaz.local.dev:8891/
  queueServiceUri:  https://stdevlocal001.queue.storage.topaz.local.dev:8893/

Service Bus namespace sbdevlocal:
  connectionString: Endpoint=sb://sbdevlocal.servicebus.topaz.local.dev:5671;...

Key Vault kv-dev:
  vaultUri: https://kv-dev.vault.topaz.local.dev:8898
```

Everything in that output is a real, reachable endpoint backed by a live Topaz instance. You can paste the connection strings directly into `appsettings.Development.json` or a `.env` file and start the application.

## Prompts: pre-defined stacks

For common scenarios, the MCP server exposes prompts, multi-step recipes that wire the individual tools together so you do not have to specify the sequence yourself. Invoke them by name in the chat:

**`bootstrap-topaz`**: first-time setup. Starts the container, registers a subscription, creates a resource group, and confirms health. This is the entry point before any provisioning prompt.

**`setup-functions-local-dev`**: provisions a Storage Account (for `AzureWebJobsStorage`), a Service Bus namespace and queue (for trigger), and a Key Vault with the storage connection string already stored as a secret. Returns a ready-to-paste `local.settings.json` snippet.

**`setup-event-driven-microservice`**: creates a Service Bus namespace with a command queue and an event topic (with a subscription), plus a Key Vault with the connection string. Models the write-side/read-side split directly.

**`setup-multi-tenant-fixtures`**: takes a list of tenant names and a naming prefix, then creates an isolated subscription, resource group, storage account, and Key Vault for each tenant. Useful for testing tenant isolation or seeding multi-tenant integration tests.

**`inspect-environment`**: runs a health check, lists subscriptions, and returns connection strings for every provisioned resource in one pass. Useful when you return to a session and want to know the current state.

Each prompt is a structured instruction message that tells Copilot exactly which tools to call and in which order. You supply the parameter values; the prompt handles the sequencing.

## When this is most useful

**Onboarding.** A new developer cloning a repository does not need to know which Azure services the application uses or how to set them up locally. They ask Copilot to bootstrap the environment, Copilot reads the project context, picks the right prompt, and the infrastructure is ready before they have finished reading the README.

**Integration testing setup.** Instead of maintaining a shared dev Azure subscription with manually-created test resources, each developer runs their own full local stack. Resources are created fresh per session, so tests never share state and there is nothing to clean up in a shared environment.

**Infrastructure experimentation.** Testing a new Service Bus topology or a multi-tenant naming convention against a real API before committing to it is fast when the infrastructure is local. Create it, test the application behaviour, tear it down, adjust the design, and repeat, all in one chat session.

**CI.** The same MCP workflow that provisions resources locally can provision them inside a CI job. The `GetConnectionStrings` output feeds directly into the test runner environment. Topaz starts in a Docker container in the CI step, the MCP server provisions what the tests need, the tests run against real emulated endpoints.

## Tearing down

When you are done with a session:

> "Stop Topaz and clean up the containers."

Copilot calls `StopTopazContainer`, which stops the Topaz host container, the `topaz-dns` resolver container, and removes the `topaz-net` network. The next session starts fresh.

If you just want to check what is running before deciding whether to stop it:

> "Is Topaz running and which services are up?"

`GetTopazStatus` hits the health endpoint and probes all service ports, returning a per-service reachability report. Useful when something is not behaving as expected and you want to rule out an infrastructure problem before looking at the application code.

## The design constraint that shaped everything

The difference that mattered in practice was that the MCP server called real APIs and returned real data. When `CreateKeyVault` returns a vault URI, that URI resolves to a live endpoint. When `GetConnectionStrings` returns a connection string, it connects to a live Service Bus namespace. The assistant is not simulating infrastructure. It is creating it.

That distinction is what makes the DNS and certificate setup matter. A generated `az keyvault create` command does not care whether the domain resolves inside a container. A real `SecretClient` call to `https://kv-dev.vault.topaz.local.dev:8898` does. The dnsmasq sidecar and the pre-installed certificate are not operational overhead. They are the thing that makes the data the assistant returns actually usable by an application.

:::tip[Get started]
The MCP server image is `thecloudtheory/topaz-mcp`. All available tags and the full tool reference are in the [MCP server docs](https://topaz.thecloudtheory.com/docs/mcp-server).

```bash
docker network create --subnet 172.28.0.0/16 topaz-net
```

[Full setup guide →](https://topaz.thecloudtheory.com/docs/mcp-server) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

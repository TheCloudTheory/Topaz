---
sidebar_position: 3
description: Build and test an AI agent that uses Azure Blob Storage, Key Vault, and Service Bus as tools — all running locally against Topaz. No cloud round-trips, no quota consumed, fully repeatable.
keywords: [ai agent local azure, semantic kernel topaz, langchain azure local, ai agent testing azure, topaz ai agent, local llm azure tools, azure sdk ai agent local, agentic loop topaz]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# AI agent development with Topaz

In this tutorial you will build a Semantic Kernel agent that calls Azure services as tools — uploading documents to Blob Storage, reading secrets from Key Vault, and publishing events to Service Bus. Every tool call runs against a local Topaz environment, giving you a deterministic, blast-radius-free harness for agent development and evaluation.

This is the workflow shown in the [AI agent use case](/use-cases#safe-local-harness-for-ai-agent-development).

## What you will build

- A Topaz environment seeded with known data (a blob, a secret, a queue)
- Three Semantic Kernel plugins backed by real Azure SDK clients pointing at Topaz
- An agent loop that reasons over a task and calls the tools
- An xUnit evaluation test that verifies the agent's tool calls are correct and deterministic

A complete runnable example is available in [`Examples/Topaz.Example.AgentHarness`](https://github.com/TheCloudTheory/Topaz/tree/main/Examples/Topaz.Example.AgentHarness).

## Prerequisites

- Topaz installed and running (see [Getting started](../intro.md))
- DNS setup completed and Topaz certificate trusted
- Azure CLI installed (`az --version`) and Topaz cloud registered (see [Azure CLI integration](../integrations/azure-cli-integration.md))
- .NET 10 SDK installed
- An OpenAI API key **or** a locally running LLM (e.g. [Ollama](https://ollama.com/) with `llama3`)

## Step 1: Start Topaz and provision resources

:::note
Topaz must be running before the steps below. See [Getting started](../intro.md) if you have not set it up yet.
:::

```bash
# Activate the Topaz cloud (run once per terminal session)
az cloud set -n Topaz
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
az account set --subscription 00000000-0000-0000-0000-000000000001

# Resource group
az group create --name rg-agent --location westeurope

# Blob Storage — seed with a customer profile document
az storage account create \
  --name stagent \
  --resource-group rg-agent \
  --location westeurope \
  --sku Standard_LRS

STORAGE_KEY=$(az storage account keys list \
  --account-name stagent \
  --resource-group rg-agent \
  --query "[0].value" --output tsv)

az storage container create \
  --name customer-data \
  --account-name stagent \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://stagent.blob.storage.topaz.local.dev:8891"

echo '{"customerId":"acme","tier":"premium","creditLimit":50000}' \
  | az storage blob upload \
      --container-name customer-data \
      --name "acme/profile.json" \
      --account-name stagent \
      --account-key "$STORAGE_KEY" \
      --blob-endpoint "https://stagent.blob.storage.topaz.local.dev:8891" \
      --data -

# Key Vault — store the payment API key
az keyvault create \
  --name kv-agent \
  --resource-group rg-agent \
  --location westeurope

az keyvault secret set \
  --vault-name kv-agent \
  --name payment-api-key \
  --value "sk-local-test-key-abc123"

# Service Bus — onboarding events queue
az servicebus namespace create \
  --name sbns-agent \
  --resource-group rg-agent \
  --location westeurope \
  --sku Standard

az servicebus queue create \
  --name onboarding-events \
  --namespace-name sbns-agent \
  --resource-group rg-agent
```

## Step 2: Create the agent project

```bash
dotnet new console -n AgentHarness
cd AgentHarness

dotnet add package Microsoft.SemanticKernel
dotnet add package Azure.Storage.Blobs
dotnet add package Azure.Security.KeyVault.Secrets
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Azure.Identity
dotnet add package TheCloudTheory.Topaz.Identity
```

## Step 3: Implement the Azure SDK plugins

Each plugin is a Semantic Kernel `KernelPlugin` backed by a real Azure SDK client. The clients point at Topaz — in production you replace the URIs with real Azure endpoints.

```csharp
// Plugins/BlobPlugin.cs
using Azure.Storage.Blobs;
using Microsoft.SemanticKernel;
using Topaz.Identity;

public class BlobPlugin
{
    private readonly BlobServiceClient _blobService;

    public BlobPlugin()
    {
        _blobService = new BlobServiceClient(
            new Uri("https://stagent.blob.storage.topaz.local.dev:8891"),
            new AzureLocalCredential());
    }

    [KernelFunction("blob_read")]
    [Description("Read a blob from Blob Storage and return its content as a string.")]
    public async Task<string> ReadBlobAsync(
        [Description("Container name")] string container,
        [Description("Blob path")] string blobPath)
    {
        var blob = _blobService
            .GetBlobContainerClient(container)
            .GetBlobClient(blobPath);

        var result = await blob.DownloadContentAsync();
        return result.Value.Content.ToString();
    }

    [KernelFunction("blob_upload")]
    [Description("Upload a string payload to Blob Storage.")]
    public async Task UploadBlobAsync(
        [Description("Container name")] string container,
        [Description("Blob path")] string blobPath,
        [Description("Content to upload")] string content)
    {
        var blob = _blobService
            .GetBlobContainerClient(container)
            .GetBlobClient(blobPath);

        await blob.UploadAsync(BinaryData.FromString(content), overwrite: true);
    }
}
```

```csharp
// Plugins/KeyVaultPlugin.cs
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.SemanticKernel;
using Topaz.Identity;

public class KeyVaultPlugin
{
    private readonly SecretClient _secrets;

    public KeyVaultPlugin()
    {
        _secrets = new SecretClient(
            new Uri("https://kv-agent.vault.topaz.local.dev:8898"),
            new AzureLocalCredential());
    }

    [KernelFunction("keyvault_get_secret")]
    [Description("Retrieve a secret from Key Vault by name.")]
    public async Task<string> GetSecretAsync(
        [Description("Secret name")] string name)
    {
        var secret = await _secrets.GetSecretAsync(name);
        return secret.Value.Value;
    }
}
```

```csharp
// Plugins/ServiceBusPlugin.cs
using Azure.Messaging.ServiceBus;
using Microsoft.SemanticKernel;

public class ServiceBusPlugin
{
    private const string ConnectionString =
        "Endpoint=sb://sbns-agent.servicebus.topaz.local.dev:8889;" +
        "SharedAccessKeyName=RootManageSharedAccessKey;" +
        "SharedAccessKey=SAS_KEY_VALUE;" +
        "UseDevelopmentEmulator=true;";

    [KernelFunction("servicebus_send")]
    [Description("Publish a JSON message to a Service Bus queue.")]
    public async Task SendAsync(
        [Description("Queue name")] string queue,
        [Description("JSON body to send")] string body)
    {
        await using var client = new ServiceBusClient(ConnectionString);
        await using var sender = client.CreateSender(queue);
        await sender.SendMessageAsync(new ServiceBusMessage(body)
        {
            ContentType = "application/json",
        });
    }
}
```

## Step 4: Wire up the agent

```csharp
// Program.cs
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = Kernel.CreateBuilder();

// Add plugins
builder.Plugins.AddFromType<BlobPlugin>("Blob");
builder.Plugins.AddFromType<KeyVaultPlugin>("KeyVault");
builder.Plugins.AddFromType<ServiceBusPlugin>("ServiceBus");

// Add LLM — use OpenAI or a local Ollama endpoint
builder.AddOpenAIChatCompletion(
    modelId: "gpt-4o-mini",
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);

// ── For Ollama (fully offline): ───────────────────────────────────
// builder.AddOpenAIChatCompletion(
//     modelId: "llama3",
//     endpoint: new Uri("http://localhost:11434/v1"),
//     apiKey: "ollama");

var kernel = builder.Build();

// Run a task that exercises all three tools
var result = await kernel.InvokePromptAsync(
    """
    A new customer "acme" needs to be onboarded. Do the following:
    1. Read their profile from blob storage: container="customer-data", blob="acme/profile.json"
    2. Retrieve the payment API key from Key Vault: secret="payment-api-key"
    3. Publish an onboarding event to Service Bus: queue="onboarding-events",
       body={"customerId":"acme","status":"onboarded"}
    Report each step as you complete it.
    """,
    new KernelArguments(new OpenAIPromptExecutionSettings
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    }));

Console.WriteLine(result);
```

## Step 5: Write evaluation tests

Deterministic evaluation tests verify that the agent calls the correct tools with the correct arguments. Because all tool calls go to Topaz — not a live cloud — the results are stable across every run.

```csharp
// AgentHarness.Tests/AgentEvalTests.cs
using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using Topaz.Identity;
using Xunit;

public class AgentEvalTests
{
    [Fact]
    public async Task OnboardingTask_ShouldUploadResultAndPublishEvent()
    {
        // Arrange: seed blob and secret (or assume provisioner has done it)

        // Act: run the agent
        await RunAgentOnboardingAsync("acme");

        // Assert blob was written
        var blobClient = new BlobServiceClient(
            new Uri("https://stagent.blob.storage.topaz.local.dev:8891"),
            new AzureLocalCredential());

        var blob = blobClient
            .GetBlobContainerClient("customer-data")
            .GetBlobClient("acme/onboarding-result.json");

        Assert.True(await blob.ExistsAsync());

        // Assert event was published
        var connectionString =
            "Endpoint=sb://sbns-agent.servicebus.topaz.local.dev:8889;" +
            "SharedAccessKeyName=RootManageSharedAccessKey;" +
            "SharedAccessKey=SAS_KEY_VALUE;" +
            "UseDevelopmentEmulator=true;";

        await using var busClient = new ServiceBusClient(connectionString);
        await using var receiver = busClient.CreateReceiver("onboarding-events");

        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(message);
        Assert.Contains("\"customerId\":\"acme\"", message.Body.ToString());
        await receiver.CompleteMessageAsync(message);
    }

    private static Task RunAgentOnboardingAsync(string customerId)
    {
        // Re-run the agent kernel with the same prompt — or extract to a shared helper
        throw new NotImplementedException("Wire to your kernel invocation");
    }
}
```

## Step 6: Offline mode with a local LLM

Replace the OpenAI client with an Ollama endpoint to run the entire agentic loop offline:

```bash
# Pull and start Ollama with a tool-capable model
ollama pull llama3.1
ollama serve
```

```csharp
// In Program.cs — replace the AddOpenAIChatCompletion call:
builder.AddOpenAIChatCompletion(
    modelId: "llama3.1",
    endpoint: new Uri("http://localhost:11434/v1"),
    apiKey: "ollama");   // Ollama ignores the API key
```

With Ollama running and Topaz as the Azure backend, the entire development loop — LLM reasoning + Azure tool calls — runs fully offline.

## Step 7: Switching to production

When deploying to real Azure:
1. Replace `AzureLocalCredential` with `DefaultAzureCredential` (or managed identity)
2. Replace Topaz URIs with real Azure resource endpoints
3. Remove `AZURE_CORE_INSTANCE_DISCOVERY=false`
4. Replace the Ollama endpoint with your OpenAI / Azure OpenAI deployment

All plugin code, kernel configuration, and evaluation logic remain unchanged.

## Common issues

| Symptom | Fix |
|---|---|
| `AuthenticationFailedException` | `AzureLocalCredential` requires the Topaz cloud to be active in the Azure CLI — run `az cloud set -n Topaz && az login` |
| `BlobServiceClient` returns 404 | Container not yet created — run the provisioner steps first |
| Agent does not call tools | Some smaller models ignore tool schemas — use `llama3.1` with Ollama or `gpt-4o-mini` with OpenAI |
| Service Bus `MessagingEntityNotFoundException` | Queue not provisioned — check the Azure CLI steps in Step 1 |

## What you've built

A fully local AI agent harness where:
- The agent's Azure tool calls hit real SDK behaviour (not mocks)
- All state is deterministic and seeded — the same data is available every run
- Evaluation tests assert tool call results without flakiness from cloud latency or quota
- The loop runs fully offline with Ollama + Topaz, no internet connection required

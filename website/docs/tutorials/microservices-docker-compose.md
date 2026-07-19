---
sidebar_position: 2
description: Build a microservices application with Service Bus, Blob Storage, Key Vault and Event Hub — all running locally in a single Topaz container via Docker Compose. No cloud subscription needed.
keywords: [topaz docker compose, microservices local azure, docker compose azure emulator, topaz microservices, local service bus docker compose, azure local development docker, topaz docker]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Microservices with Docker Compose

In this tutorial you will build a three-service microservices application that uses Azure Service Bus for messaging, Blob Storage for file persistence, and Key Vault for secrets — all backed by a single Topaz container in Docker Compose. No cloud subscription is needed, and a new team member can onboard with a single `docker compose up`.

This is the workflow shown in the [Microservices use case](/use-cases#replace-a-cloud-subscription-with-one-topaz-container).

## What you will build

```
┌──────────────────────────────────────────────────────────┐
│  docker-compose.yml                                      │
│                                                          │
│  ┌─────────────┐   ┌──────────────┐   ┌──────────────┐   │
│  │ api-gateway │   │ order-service│   │notify-service│   │
│  └──────┬──────┘   └──────┬───────┘   └──────┬───────┘   │
│         │                 │                  │           │
│         └─────────────────┼──────────────────┘           │
│                           ▼                              │
│              ┌────────────────────────┐                  │
│              │  topaz (single image)  │                  │
│              │  • Service Bus         │                  │
│              │  • Blob Storage        │                  │
│              │  • Key Vault           │                  │
│              └────────────────────────┘                  │
└──────────────────────────────────────────────────────────┘
```

- `api-gateway` — receives HTTP requests and publishes order events to Service Bus
- `order-service` — consumes events, stores order documents in Blob Storage
- `notify-service` — consumes events, reads an API key from Key Vault, sends a notification

## Prerequisites

- Docker Desktop (or Docker Engine + Compose plugin)
- .NET 10 SDK
- Azure CLI (for local provisioning)

## Project structure

```
compose-demo/
  cloud.json            # Download from the Topaz repo (one-time)
  docker-compose.yml
  init/
    provision.sh          # Provisions Topaz resources on startup
  src/
    ApiGateway/
    OrderService/
    NotifyService/
    Shared/
```

## Step 1: Write the Docker Compose file

```yaml
# docker-compose.yml
services:

  # ── Topaz (single Azure emulator container) ──────────────────────
  topaz:
    image: thecloudtheory/topaz-host:latest
    container_name: topaz.local.dev
    hostname: topaz.local.dev
    ports:
      - "8891:8891"   # Blob Storage
      - "8889:8889"   # Service Bus (plain AMQP)
      - "5671:5671"   # Service Bus (TLS AMQP)
      - "8898:8898"   # Key Vault
      - "8899:8899"   # ARM / Resource Manager + auth
    command:
      - --default-subscription
      - 00000000-0000-0000-0000-000000000001
      - --log-level
      - Warning
    volumes:
      - topaz-data:/app/data   # Persist state across restarts
    healthcheck:
      test: ["CMD", "curl", "-sk", "https://localhost:8899/"]
      interval: 5s
      timeout: 5s
      retries: 10

  # ── Provisioner (runs once, then exits) ──────────────────────────
  provisioner:
    image: mcr.microsoft.com/azure-cli:latest
    depends_on:
      topaz:
        condition: service_healthy
    volumes:
      - ./init:/init
      - ./cloud.json:/cloud.json
    command: bash /init/provision.sh
    environment:
      AZURE_CORE_INSTANCE_DISCOVERY: "false"

  # ── Application services ─────────────────────────────────────────
  api-gateway:
    build: ./src/ApiGateway
    depends_on:
      provisioner:
        condition: service_completed_successfully
    environment:
      SERVICE_BUS_CONNECTION: >-
        Endpoint=sb://sbns-demo.servicebus.topaz.local.dev:8889;
        SharedAccessKeyName=RootManageSharedAccessKey;
        SharedAccessKey=SAS_KEY_VALUE;
        UseDevelopmentEmulator=true;
      AZURE_CORE_INSTANCE_DISCOVERY: "false"
    ports:
      - "5000:8080"
    extra_hosts:
      - "topaz.local.dev:host-gateway"
      - "sbns-demo.servicebus.topaz.local.dev:host-gateway"
      - "storders.blob.storage.topaz.local.dev:host-gateway"
      - "vault-demo.vault.topaz.local.dev:host-gateway"

  order-service:
    build: ./src/OrderService
    depends_on:
      provisioner:
        condition: service_completed_successfully
    environment:
      SERVICE_BUS_CONNECTION: >-
        Endpoint=sb://sbns-demo.servicebus.topaz.local.dev:8889;
        SharedAccessKeyName=RootManageSharedAccessKey;
        SharedAccessKey=SAS_KEY_VALUE;
        UseDevelopmentEmulator=true;
      BLOB_ENDPOINT: "https://storders.blob.storage.topaz.local.dev:8891"
      AZURE_CORE_INSTANCE_DISCOVERY: "false"
    extra_hosts:
      - "topaz.local.dev:host-gateway"
      - "sbns-demo.servicebus.topaz.local.dev:host-gateway"
      - "storders.blob.storage.topaz.local.dev:host-gateway"

  notify-service:
    build: ./src/NotifyService
    depends_on:
      provisioner:
        condition: service_completed_successfully
    environment:
      SERVICE_BUS_CONNECTION: >-
        Endpoint=sb://sbns-demo.servicebus.topaz.local.dev:8889;
        SharedAccessKeyName=RootManageSharedAccessKey;
        SharedAccessKey=SAS_KEY_VALUE;
        UseDevelopmentEmulator=true;
      KEY_VAULT_URI: "https://vault-demo.vault.topaz.local.dev:8898"
      AZURE_CORE_INSTANCE_DISCOVERY: "false"
    extra_hosts:
      - "topaz.local.dev:host-gateway"
      - "sbns-demo.servicebus.topaz.local.dev:host-gateway"
      - "vault-demo.vault.topaz.local.dev:host-gateway"

volumes:
  topaz-data:
```

:::note[extra_hosts]

The `extra_hosts` entries map Topaz hostnames to `host-gateway` (which resolves to the Docker host's IP, i.e. `127.0.0.1` on Linux or the Docker bridge on macOS/Windows). This gives each container the same DNS view as the host machine.

:::

:::note[cloud.json]
Place the Topaz `cloud.json` in your project root — download it once:

```bash
curl -fsSL https://raw.githubusercontent.com/TheCloudTheory/Topaz/refs/heads/main/cloud.json -o cloud.json
```

The provisioner container mounts it and uses it to register the Topaz cloud with the Azure CLI — the same file used in every other Topaz integration.
:::

## Step 2: Write the provisioner script

The provisioner runs once after Topaz is healthy, creates all required resources, and exits. Application services wait for it to complete before starting.

```bash
#!/usr/bin/env bash
# init/provision.sh
set -e

SUBSCRIPTION=00000000-0000-0000-0000-000000000001

# Register the Topaz cloud using the canonical cloud.json
az cloud register -n Topaz --cloud-config @/cloud.json 2>/dev/null || true

az cloud set -n Topaz
az login --allow-no-subscriptions
az account set --subscription "$SUBSCRIPTION"

# Resource group
az group create --name rg-demo --location westeurope

# Service Bus
az servicebus namespace create \
  --name sbns-demo \
  --resource-group rg-demo \
  --location westeurope \
  --sku Standard

az servicebus topic create \
  --name orders \
  --namespace-name sbns-demo \
  --resource-group rg-demo

az servicebus topic subscription create \
  --name order-service \
  --topic-name orders \
  --namespace-name sbns-demo \
  --resource-group rg-demo

az servicebus topic subscription create \
  --name notify-service \
  --topic-name orders \
  --namespace-name sbns-demo \
  --resource-group rg-demo

# Blob Storage
az storage account create \
  --name storders \
  --resource-group rg-demo \
  --location westeurope \
  --sku Standard_LRS

STORAGE_KEY=$(az storage account keys list \
  --account-name storders \
  --resource-group rg-demo \
  --query "[0].value" --output tsv)

az storage container create \
  --name order-documents \
  --account-name storders \
  --account-key "$STORAGE_KEY" \
  --blob-endpoint "https://storders.blob.storage.topaz.local.dev:8891"

# Key Vault + secret
az keyvault create \
  --name vault-demo \
  --resource-group rg-demo \
  --location westeurope

az keyvault secret set \
  --vault-name vault-demo \
  --name notification-api-key \
  --value "local-api-key-abc123"

echo "Provisioning complete."
```

## Step 3: Connect application services to Topaz

Each service reads its connection details from environment variables injected by Docker Compose. The SDK code itself is identical to what you would use against real Azure.

<Tabs groupId="service">
<TabItem value="api-gateway" label="API Gateway">

```csharp
// Publishes to the "orders" topic on Service Bus
builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration["SERVICE_BUS_CONNECTION"]!;
    return new ServiceBusClient(connectionString);
});

app.MapPost("/orders", async (ServiceBusClient bus, OrderRequest req) =>
{
    var sender = bus.CreateSender("orders");
    await sender.SendMessageAsync(
        new ServiceBusMessage(JsonSerializer.Serialize(req))
        {
            ContentType = "application/json",
            Subject = "OrderPlaced",
        });
    return Results.Accepted();
});
```

</TabItem>
<TabItem value="order-service" label="Order Service">

```csharp
// Subscribes to "orders" topic, stores document in Blob Storage
builder.Services.AddSingleton(sp =>
    new BlobServiceClient(
        new Uri(builder.Configuration["BLOB_ENDPOINT"]!),
        new AzureLocalCredential()));   // ← Topaz credential helper

// In hosted service / background worker:
await using var receiver = bus.CreateReceiver("orders", "order-service");

await foreach (var message in receiver.ReceiveMessagesAsync(cancellationToken))
{
    var order = JsonSerializer.Deserialize<OrderRequest>(message.Body)!;

    var blob = blobService
        .GetBlobContainerClient("order-documents")
        .GetBlobClient($"{order.OrderId}.json");

    await blob.UploadAsync(BinaryData.FromObjectAsJson(order), overwrite: true);
    await receiver.CompleteMessageAsync(message);
}
```

</TabItem>
<TabItem value="notify-service" label="Notify Service">

```csharp
// Reads API key from Key Vault, subscribes to "orders" topic
var keyVaultUri = new Uri(builder.Configuration["KEY_VAULT_URI"]!);
builder.Services.AddSingleton(new SecretClient(keyVaultUri, new AzureLocalCredential()));

// In hosted service:
var secret = await secretClient.GetSecretAsync("notification-api-key");
var apiKey = secret.Value.Value;   // "local-api-key-abc123"

await using var receiver = bus.CreateReceiver("orders", "notify-service");

await foreach (var message in receiver.ReceiveMessagesAsync(cancellationToken))
{
    var order = JsonSerializer.Deserialize<OrderRequest>(message.Body)!;
    await SendNotificationAsync(apiKey, order);   // your notification logic
    await receiver.CompleteMessageAsync(message);
}
```

</TabItem>
</Tabs>

## Step 4: Start the stack

```bash
docker compose up --build
```

Docker Compose will:
1. Pull and start the Topaz container
2. Wait for Topaz to pass its health check
3. Run the provisioner (creates resource group, Service Bus, storage, Key Vault)
4. Start `api-gateway`, `order-service`, and `notify-service`

Test the end-to-end flow:

```bash
# Post an order through the gateway
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"orderId": "ORD-001", "customerId": "ACME", "amount": 49.99}'
```

The order flows: API Gateway → Service Bus topic → both subscribers → Blob Storage + notification.

## Step 5: Persist state across restarts

The `topaz-data` volume defined in the Compose file persists all resource state across `docker compose down` / `up` cycles. Resources created during the first provisioner run are still available on subsequent starts.

To reset to a clean state:

```bash
docker compose down -v   # -v removes the named volume
docker compose up
```

## Step 6: Switching to real Azure

The only changes needed to point at real Azure:
1. Replace `SERVICE_BUS_CONNECTION` with the connection string from the Azure portal
2. Replace `BLOB_ENDPOINT` with `https://<account>.blob.core.windows.net`
3. Replace `KEY_VAULT_URI` with `https://<vault>.vault.azure.net`
4. Replace `AzureLocalCredential` with `DefaultAzureCredential` (or a managed identity credential)
5. Remove `extra_hosts` and `AZURE_CORE_INSTANCE_DISCOVERY`

All SDK logic — senders, receivers, blob clients, secret clients — stays unchanged.

## Common issues

| Symptom | Fix |
|---|---|
| Services start before resources exist | Provisioner has not finished — check `service_completed_successfully` condition on `depends_on` |
| `SSL certificate problem` in provisioner | Mount the Topaz certificate into the Azure CLI container and `update-ca-certificates` |
| `host-gateway` not resolved | Linux: use `host-gateway`; macOS/Windows Docker Desktop: use `host.docker.internal` instead |
| Messages not routed to subscriptions | Subscriptions were created after messages were published — provisioner runs before services start, so this should not occur if the dependency chain is correct |

## What you've built

A complete local microservices stack where:
- Azure Service Bus fans out order events to multiple consumers
- Azure Blob Storage persists order documents
- Azure Key Vault supplies API keys at runtime
- A single `docker compose up` stands everything up from scratch
- A new engineer can onboard without a cloud subscription or shared credentials

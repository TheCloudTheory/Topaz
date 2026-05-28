---
sidebar_position: 5
description: Use Topaz for Azure Service Bus local development — create a namespace, send and receive messages via queues and topics, and connect the Azure SDK and MassTransit without a real Azure subscription.
keywords: [azure service bus local, service bus local development, local service bus emulator, topaz service bus, azure service bus emulator, service bus testing local, masstransit local]
---

import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';

# Local Service Bus development with Topaz

This tutorial walks through a complete Azure Service Bus local development workflow using Topaz: create a namespace, send and receive messages on queues and topics via the Azure CLI and the Azure SDK — all without connecting to real Azure.

## What you will build

- A local Service Bus namespace running on Topaz
- A queue and a topic/subscription pair managed via the Azure CLI
- .NET and Python snippets for sending and receiving messages using `ServiceBusClient`
- A MassTransit configuration pointing at Topaz

## Prerequisites

- Topaz installed and running (see [Getting started](../intro.md))
- DNS setup completed
- Topaz certificate trusted by your OS and tooling
- Azure CLI installed (`az --version`)
- Topaz cloud registered in Azure CLI (see [Azure CLI integration](../integrations/azure-cli-integration.md))

## Step 1: Start Topaz

```bash
topaz-host \
  --default-subscription 00000000-0000-0000-0000-000000000001 \
  --log-level Information
```

## Step 2: Set the active cloud to Topaz

```bash
az cloud set -n Topaz
export AZURE_CORE_INSTANCE_DISCOVERY=false
az login
az account set --subscription 00000000-0000-0000-0000-000000000001
```

## Step 3: Create a resource group and Service Bus namespace

```bash
az group create \
  --name rg-local \
  --location westeurope

az servicebus namespace create \
  --name sbns-local \
  --resource-group rg-local \
  --location westeurope \
  --sku Standard
```

Topaz assigns the namespace a local AMQP hostname: `sbns-local.servicebus.topaz.local.dev`.

## Step 4: Create a queue

```bash
az servicebus queue create \
  --name orders \
  --namespace-name sbns-local \
  --resource-group rg-local
```

Verify the queue was created:

```bash
az servicebus queue show \
  --name orders \
  --namespace-name sbns-local \
  --resource-group rg-local \
  --output table
```

## Step 5: Create a topic and subscription

```bash
az servicebus topic create \
  --name events \
  --namespace-name sbns-local \
  --resource-group rg-local

az servicebus topic subscription create \
  --name all-events \
  --topic-name events \
  --namespace-name sbns-local \
  --resource-group rg-local
```

## Step 6: Send and receive messages — Azure SDK

<Tabs groupId="sdk-language">
<TabItem value="dotnet" label=".NET">

Install the Azure Service Bus client:

```bash
dotnet add package Azure.Messaging.ServiceBus
```

The connection string uses `UseDevelopmentEmulator=true`, which tells the SDK to connect over plain AMQP (port 8889) without TLS — the same flag used for the [Microsoft Service Bus Emulator](https://learn.microsoft.com/en-us/azure/service-bus-messaging/overview-emulator):

```csharp
using Azure.Messaging.ServiceBus;

var connectionString =
    "Endpoint=sb://sbns-local.servicebus.topaz.local.dev:8889;" +
    "SharedAccessKeyName=RootManageSharedAccessKey;" +
    "SharedAccessKey=SAS_KEY_VALUE;" +
    "UseDevelopmentEmulator=true;";

await using var client = new ServiceBusClient(connectionString);

// --- Send a message ----------------------------------------------------------
await using var sender = client.CreateSender("orders");
await sender.SendMessageAsync(new ServiceBusMessage("Order #1001"));
Console.WriteLine("Sent: Order #1001");

// --- Receive a message -------------------------------------------------------
await using var receiver = client.CreateReceiver("orders");
var message = await receiver.ReceiveMessageAsync(maxWaitTime: TimeSpan.FromSeconds(5));

if (message != null)
{
    Console.WriteLine($"Received: {message.Body}");  // Order #1001
    await receiver.CompleteMessageAsync(message);
}
```

</TabItem>
<TabItem value="python" label="Python">

Install the Topaz Python SDK and the Azure Service Bus client:

```bash
pip install topaz-sdk azure-servicebus
```

Use `TopazResourceHelpers.get_service_bus_connection_string()` to get the AMQP connection string (plain, port 8889) and interact with Service Bus through the standard `azure-servicebus` client:

```python
from azure.servicebus import ServiceBusClient, ServiceBusMessage
from topaz_sdk import TopazResourceHelpers

connection_string = TopazResourceHelpers.get_service_bus_connection_string("sbns-local")

with ServiceBusClient.from_connection_string(connection_string) as client:

    # --- Send a message ------------------------------------------------------
    with client.get_queue_sender("orders") as sender:
        sender.send_messages(ServiceBusMessage("Order #1001"))
        print("Sent: Order #1001")

    # --- Receive a message ---------------------------------------------------
    with client.get_queue_receiver("orders", max_wait_time=5) as receiver:
        for msg in receiver:
            print(f"Received: {str(msg)}")  # Order #1001
            receiver.complete_message(msg)
            break
```

:::note[Certificate trust]

Set `REQUESTS_CA_BUNDLE` to the path of the Topaz certificate so the Python HTTP stack trusts TLS connections:

```bash
export REQUESTS_CA_BUNDLE=/path/to/topaz.crt
```

:::

</TabItem>
</Tabs>

:::tip[Switching to production]

Replace the connection string with the one from the Azure portal. The SDK code — senders, receivers, message processing — is identical.

:::

## Step 7: Publish to a topic and receive from a subscription

<Tabs groupId="sdk-language">
<TabItem value="dotnet" label=".NET">

```csharp
// Publish to the "events" topic
await using var topicSender = client.CreateSender("events");
await topicSender.SendMessageAsync(new ServiceBusMessage("UserRegistered"));

// Receive from the "all-events" subscription
await using var subReceiver = client.CreateReceiver("events", "all-events");
var event1 = await subReceiver.ReceiveMessageAsync(maxWaitTime: TimeSpan.FromSeconds(5));

if (event1 != null)
{
    Console.WriteLine($"Subscription received: {event1.Body}");
    await subReceiver.CompleteMessageAsync(event1);
}
```

</TabItem>
<TabItem value="python" label="Python">

```python
with ServiceBusClient.from_connection_string(connection_string) as client:

    # Publish to the "events" topic
    with client.get_topic_sender("events") as sender:
        sender.send_messages(ServiceBusMessage("UserRegistered"))

    # Receive from the "all-events" subscription
    with client.get_subscription_receiver("events", "all-events", max_wait_time=5) as receiver:
        for msg in receiver:
            print(f"Subscription received: {str(msg)}")
            receiver.complete_message(msg)
            break
```

</TabItem>
</Tabs>

## Step 8: Using MassTransit

MassTransit communicates over AMQP with TLS (port 5671). Use the TLS connection string variant instead:

Install the MassTransit Azure Service Bus transport:

```bash
dotnet add package MassTransit.Azure.ServiceBus.Core
```

Configure the host:

```csharp
using MassTransit;

services.AddMassTransit(x =>
{
    x.AddConsumer<OrderConsumer>();

    x.UsingAzureServiceBus((context, cfg) =>
    {
        // TLS connection string — port 5671, no UseDevelopmentEmulator flag
        cfg.Host(
            "Endpoint=sb://sbns-local.servicebus.topaz.local.dev:5671;" +
            "SharedAccessKeyName=RootManageSharedAccessKey;" +
            "SharedAccessKey=SAS_KEY_VALUE;");

        cfg.ReceiveEndpoint("orders", e =>
        {
            e.ConfigureConsumer<OrderConsumer>(context);
        });
    });
});
```

Define a consumer:

```csharp
public class OrderConsumer : IConsumer<OrderMessage>
{
    public Task Consume(ConsumeContext<OrderMessage> context)
    {
        Console.WriteLine($"Processing order: {context.Message.OrderId}");
        return Task.CompletedTask;
    }
}

public record OrderMessage(string OrderId);
```

:::note[Certificate trust for MassTransit]

MassTransit uses the standard .NET TLS stack, so the Topaz certificate must be trusted at the OS level. Follow the certificate trust steps in [Getting started](../intro.md).

:::

## Step 9: Automate with Testcontainers

When running tests, start Topaz automatically using [Testcontainers](../ecosystem/testcontainers.md):

```csharp
var container = new ContainerBuilder("thecloudtheory/topaz-host:latest")
    .WithPortBinding(8889)  // plain AMQP (UseDevelopmentEmulator)
    .WithPortBinding(5671)  // AMQPS/TLS (MassTransit)
    .WithPortBinding(8899)  // ARM / Resource Manager
    .Build();

await container.StartAsync();
```

Then create the namespace and queue via the ARM SDK before each test class, using the same provisioning pattern shown in the [Testcontainers guide](../ecosystem/testcontainers.md).

## Common gotchas

| Symptom | Cause | Fix |
|---|---|---|
| `Connection refused` on port 8889 | `UseDevelopmentEmulator=true` required but missing from connection string | Add `UseDevelopmentEmulator=true;` to the connection string |
| `SSL handshake failed` in MassTransit | Topaz certificate not trusted at OS level | Trust the certificate as described in [Getting started](../intro.md) |
| `MessagingEntityNotFoundException` | Queue or topic does not exist yet | Create the queue/topic first via the Azure CLI or ARM SDK |
| MassTransit connects but never receives | Wrong port — MassTransit needs port 5671 (TLS), not 8889 | Use `GetServiceBusConnectionStringWithTls(...)` or point to port 5671 |

---
slug: amqp-compatibility-local-azure-emulator
title: "What AMQP compatibility means for a local Azure emulator (.NET / MassTransit)"
description: Getting AMQP 1.0 right in a local Azure emulator is harder than it looks. This post covers what real AMQP compatibility means, lock settlement, receive credit, management links, and how Topaz handles it well enough to run MassTransit end-to-end. Examples use C# and MassTransit.
keywords: [amqp azure emulator, service bus local development, masstransit local emulator, masstransit azure service bus local, amqp 1.0 emulator, azure service bus emulator alternative, topaz service bus]
authors: kamilmrzyglod
tags: [general, service-bus]
---

I wanted to see whether Topaz could run a real PeekLock consumer, not just accept AMQP frames and pass a basic SDK smoke test. The first MassTransit run failed in two different ways. `CompleteAsync` waited 60 seconds for a management response that never arrived, and after fixing that, the consumer still stalled after a single message.

That was the point where "supports AMQP" stopped being a useful statement. This post explains what MassTransit was actually doing on the wire, which parts of the protocol Topaz was still missing, and which traces made the root causes obvious.

The concrete examples use MassTransit and the Azure Service Bus SDK for .NET. The AMQP behaviour described applies to any framework driving PeekLock, but the code is C#. If you are not working in .NET, the protocol sections may still be useful context for evaluating any AMQP emulator.

{/* truncate */}

## The two layers of Service Bus compatibility

Most discussions of Azure Service Bus compatibility focus on the control plane: can you create namespaces, queues, and topics through ARM or the Azure CLI? That layer is important, it is what makes `az servicebus queue create` and `azurerm_servicebus_queue` work locally, but it is not the interesting layer for message-processing code.

The interesting layer is the AMQP data plane, and it breaks down into two sub-layers:

**SDK compatibility**: does the Azure Service Bus SDK connect, authenticate, send, and receive? This is the easier bar. The SDK connects through CBS (Claims-Based Security), opens a sender link for sending and a receiver link for receiving, and uses basic settled transfers for most operations. Topaz already handled this layer before the MassTransit work, which is why straightforward SDK send and receive scenarios were working.

**Framework compatibility**: does a message-processing framework like MassTransit, NServiceBus, or Rebus actually work on top of it? Frameworks drive a more complete subset of the AMQP specification. They open management links alongside receive links, use `$management` request-response to perform operations the SDK does not surface directly, expect unsettled transfers with explicit client-side settlement, and rely on correct credit replenishment to maintain throughput. For a framework-driven consumer, these behaviors are the normal operating path.

That distinction mattered immediately. Passing the SDK path had not told me anything about whether MassTransit would keep consuming messages.

## What MassTransit actually does over AMQP

MassTransit's Azure Service Bus transport (`MassTransit.Azure.ServiceBus.Core`) uses the Azure SDK as its underlying client but adds a layer of messaging conventions on top. When a `ReceiveEndpoint` starts, it:

1. Opens an AMQP session and a receiver link to the queue.
2. Immediately opens a second link to `<queue>/$management`, a request-response link used for management operations like `com.microsoft:update-disposition` and `com.microsoft:renew-lock`.
3. Sends an AMQP `FLOW` frame on the receiver link with initial link credit, indicating how many messages it is prepared to accept.
4. For every message it processes, sends a `DISPOSITION` frame to complete or abandon it, then expects the broker to update the session's delivery state and replenish credit.

Step 2 is where most partial AMQP implementations break down. The Azure Service Bus SDK does not surface queue-level `$management` directly to callers; it is an internal transport detail. The root `$management` link is handled by `IRequestProcessor` in most AMQP server implementations, but queue-scoped `$management` links are distinct. They attach to the queue's link processor, not the root processor. An emulator that routes all `$management` traffic to one handler will complete the CBS authentication but silently drop every queue management request, causing MassTransit's `CompleteAsync` to wait 60 seconds for a response that never arrives.

Step 4 is where the second class of failures appears. If the broker sends transfers as sender-settled (the `settled` bit set in the TRANSFER frame), the receiver never adds the delivery to its unsettled map. When MassTransit calls `CompleteAsync`, the SDK sees no pending delivery with that lock token, settlement happens locally without waiting for broker confirmation, and no `DISPOSITION` frame is sent. The broker never gets the acknowledgement it expects. Credit is consumed but never replenished. After the first message, the consumer stops receiving.

## The bugs we found

Running MassTransit against an early version of Topaz exposed exactly these failure modes. This summary is cleaner than the debugging felt at the time. I first assumed the missing management response was the whole problem. It was not.

**Bug 1: Missing queue `$management` handler.** Topaz already handled the root `$management` link for CBS token validation. Queue-scoped management links were being attached to the link processor without a handler. MassTransit's `CompleteAsync` calls timed out after 60 seconds with an `amqp:internal-error`.

The fix required intercepting `ATTACH` frames addressed to `<anything>/$management` inside `LinkProcessor` and routing them to a dedicated request-response endpoint, separate from the root management handler. On the sender side, the endpoint registers a request processor that reads the `operation` property from incoming application properties, builds the appropriate response (status code, correlation ID, operation-specific payload for `com.microsoft:renew-lock`), and sends it back on the paired response link.

**Bug 2: Wrong management response property names.** Once queue management requests were being answered, MassTransit's completion path started working, but threw `amqp:internal-error (GeneralError)` instead of returning. Decompiling the Azure SDK revealed the issue:

```csharp
public static AmqpResponseStatusCode GetResponseStatusCode(this AmqpMessage responseMessage)
{
    // reads responseMessage.ApplicationProperties.Map["statusCode"]
}
```

The initial management responses used `status-code` and `status-description`, the property names from the CBS specification. Service Bus management uses camelCase: `statusCode` and `statusDescription`. The SDK parsed the response, found no `statusCode` key, and treated the reply as a failure. Changing two string literals fixed it.

**Bug 3: Sender-settled transfers.** After the response key fix, the `amqp:internal-error` disappeared. At that point I thought the transport was fixed. It was not. Only the first message was consumed, then the consumer sat idle. Looking at the AMQP frame trace showed: one `FLOW` frame from the consumer link, one outgoing `TRANSFER`, then silence. No new `FLOW` frame, no credit replenishment.

The cause was in `OutgoingLinkEndpoint`. Topaz was setting the `Settled` property on outgoing deliveries to `true`, which made them sender-settled transfers. The SDK never put the delivery in the unsettled map. `CompleteAsync` short-circuited. No `DISPOSITION` came back. No credit was restored. The fix was a one-line change:

```csharp
// Before: sender-settled, credit consumed without replenishment
DeliverySettledProperty.SetValue(delivery, true);

// After: unsettled, let the receiver settle explicitly via DISPOSITION
DeliverySettledProperty.SetValue(delivery, false);
```

The consumer now receives a message, completes it, and the broker sees the `DISPOSITION` and returns credit. That is standard PeekLock behaviour.

## What the working example looks like

The `Topaz.Examples.MassTransit` project in the repository is a minimal ASP.NET Core app that starts a Topaz container via Testcontainers, provisions a Service Bus namespace and queue via the ARM API, and wires up MassTransit with a consumer:

```csharp
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MessageConsumer>();
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(TopazResourceHelpers.GetServiceBusConnectionStringWithTls("sbnamespace"));
        cfg.ReceiveEndpoint("sbqueue", e =>
        {
            e.ConfigureConsumer<MessageConsumer>(context);
            e.PrefetchCount = 1;
        });
    });
});
```

MassTransit uses the TLS endpoint (port 5671) because it expects a standard Azure Service Bus connection string without `UseDevelopmentEmulator=true`. The non-TLS endpoint (port 8889) uses pre-settled receive-and-delete semantics, which is compatible with the Azure SDK's development emulator mode but not with how MassTransit drives the receive path. For any framework that manages its own PeekLock cycle, the TLS endpoint is the right choice.

The worker sends one message per second. With the three bugs above fixed, the output is exactly one `Message dispatched` and one `Message consumed` per second, sustained indefinitely:

```
Message dispatched: {"Timestamp":"2026-05-26T09:14:35.86...","Message":"The time is ..."}
Message consumed:   {"Timestamp":"2026-05-26T09:14:35.86...","Message":"The time is ..."}
Message dispatched: {"Timestamp":"2026-05-26T09:14:36.97...","Message":"The time is ..."}
Message consumed:   {"Timestamp":"2026-05-26T09:14:36.97...","Message":"The time is ..."}
```

## Why MassTransit support is a meaningful signal

MassTransit is not the only framework that exercises this part of the AMQP specification, but it is a widely-used one in the .NET ecosystem and it drives all three of the failure modes described above simultaneously. If Topaz runs MassTransit end-to-end, it means:

- Queue-scoped `$management` request-response is implemented and returns correct responses.
- Transfers are unsettled, so receivers that manage their own settlement cycle work correctly.
- Credit replenishment is functional, so sustained throughput is possible without the consumer stalling.

NServiceBus's Azure Service Bus transport and any other library that implements the full PeekLock cycle over the Azure SDK should follow the same path.

What this does **not** guarantee: dead-letter queues, message sessions, topic subscription rules, and partitioned entities are not yet implemented. If your application depends on those features, the Azure Service Bus Emulator still has the advantage on that specific surface. Topaz's [roadmap](/roadmap) tracks when those features are coming.

## The comparison with the Azure Service Bus Emulator

The Microsoft emulator ships as two Docker containers (emulator plus SQL Server), configures entities via a static `config.json` at startup, and does not implement the ARM control plane. What it does have is a more complete messaging feature set at the moment: dead-letter queues, message sessions, and topic filters work today.

The boundary between the two emulators is simple. If you need `az servicebus queue create` to work locally, Terraform `azurerm_servicebus_queue` to apply locally, or multiple namespaces in the same environment, the Microsoft emulator cannot help. It has no ARM API. If you need dead-letter queues or message sessions, Topaz cannot yet help.

For teams who need a real PeekLock consumer with sustained receive throughput and ARM-level infrastructure tooling in the same local process, Topaz is the current answer.

:::tip[Try it]
The `Topaz.Examples.MassTransit` example is in the repository. Run it with:

```bash
cd Examples/Topaz.Examples.MassTransit && dotnet run
```

It starts Topaz via Testcontainers, provisions the namespace and queue, runs the direct SDK test, then starts the MassTransit consumer. No Azure subscription required.

[Service Bus Emulator comparison →](/docs/comparisons/service-bus-emulator-alternative) · [Star on GitHub →](https://github.com/TheCloudTheory/Topaz)
:::

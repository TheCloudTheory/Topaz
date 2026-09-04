using System.Net.Http.Json;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.EventGrid.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.EventGrid;

internal sealed class EventGridEventDeliveryBackgroundService(
    SubscriptionControlPlane subscriptionControlPlane,
    EventGridTopicControlPlane controlPlane,
    HttpClient client,
    ITopazLogger logger,
    TimeSpan interval) : ITopazBackgroundService
{
    private static readonly string ValidatedSubscriptionSubresource =
        nameof(Subresource.ValidatedSubscriptions).ToLowerInvariant();

    private static readonly string EventSubresource =
        nameof(Subresource.Events).ToLowerInvariant();

    private readonly EventGridTopicResourceProvider _provider = new(logger);

    public string Name => "Event Grid Event Delivery";
    public DateTimeOffset? ExecutedAt { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug(nameof(EventGridEventDeliveryBackgroundService), nameof(StartAsync),
            "Message expiry scheduler started (interval: {0})", interval);

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await DeliverEvents(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown — exit gracefully
        }
    }

    private async Task DeliverEvents(CancellationToken cancellationToken)
    {
        logger.LogDebug(nameof(EventGridEventDeliveryBackgroundService), nameof(DeliverEvents),
            "Starting event delivery background service.");

        var subscriptions = subscriptionControlPlane.List();
        if (subscriptions.Result != OperationResult.Success)
        {
            logger.LogError(nameof(EventGridEventDeliveryBackgroundService), nameof(DeliverEvents),
                "Failed to list subscriptions.");
            return;
        }

        foreach (var subscription in subscriptions.Resource!)
        {
            var topicsOperation = controlPlane.ListBySubscription(subscription.ToSubscriptionIdentifier(), null);
            if (topicsOperation.Result != OperationResult.Success)
            {
                logger.LogError(nameof(EventGridEventDeliveryBackgroundService), nameof(DeliverEvents),
                    "Failed to list topics.");
                continue;
            }

            foreach (var topic in topicsOperation.Resource!)
            {
                var eventSubscriptionsOperation =
                    controlPlane.ListEventSubscriptions(topic.GetSubscription(), topic.GetResourceGroup(), topic.Name,
                        null);

                if (eventSubscriptionsOperation.Result != OperationResult.Success)
                {
                    logger.LogError(nameof(EventGridEventDeliveryBackgroundService), nameof(DeliverEvents),
                        "Failed to list event subscriptions.");
                }

                var events = _provider.ListSubresourcesAs<EventGridEventEnvelope<object>>(topic.GetSubscription(),
                    topic.GetResourceGroup(), topic.Name, EventSubresource);

                foreach (var eventSubscription in eventSubscriptionsOperation.Resource!)
                {
                    var destination = eventSubscription.Properties.Destination;
                    if (destination == null)
                    {
                        logger.LogDebug(nameof(EventGridEventDeliveryBackgroundService), nameof(StartAsync),
                            "Event Grid topic subscription destination is null. Skipping");
                        continue;
                    }

                    var destinationType = destination.EndpointType;
                    switch (destinationType)
                    {
                        case "WebHook":
                            await DeliverWebHookEvent(topic.GetSubscription(), topic.GetResourceGroup(), topic,
                                eventSubscription.Name, destination, events, cancellationToken);
                            break;
                        default:
                            logger.LogWarning(
                                $"Event Grid topic subscription destination type '{destinationType}' is not supported yet.");
                            continue;
                    }
                }
            }
        }

        ExecutedAt = DateTimeOffset.UtcNow;
        logger.LogDebug(nameof(EventGridEventDeliveryBackgroundService), nameof(DeliverEvents),
            "Event delivery background service completed.");
    }

    private async Task DeliverWebHookEvent<TEventModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, EventGridTopicResource topic, string subscriptionName,
        EventSubscriptionDestination destination,
        EventGridEventEnvelope<TEventModel>[] data,
        CancellationToken cancellationToken)
    {
        var endpointUrl = destination.Properties!.EndpointUrl;
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            throw new InvalidOperationException(
                "Event Grid topic subscription destination endpoint URL is null or empty.");
        }

        var validatedSubscription = _provider.GetSubresourceAs<ValidatedEventSubscription>(subscriptionIdentifier,
            resourceGroupIdentifier, subscriptionName, topic.Name, ValidatedSubscriptionSubresource);

        // Event Grid always validates the destination endpoint URL.
        // If there's no validated subscription, first we need to send a validation request.
        var message = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        if (validatedSubscription == null)
        {
            await HandleSubscriptionValidationRequest(subscriptionIdentifier, resourceGroupIdentifier, topic.Name, topic.Id, subscriptionName, message, cancellationToken);
            return;
        }
        
        await SendEventDataWithDeliveryStatus(subscriptionIdentifier, resourceGroupIdentifier, topic.Name, subscriptionName, data, message, cancellationToken);
    }

    private async Task SendEventDataWithDeliveryStatus<TEventModel>(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, string subscriptionName,
        EventGridEventEnvelope<TEventModel>[] data, HttpRequestMessage message, CancellationToken cancellationToken)
    {
        message.Content = JsonContent.Create(data.Select(e => e.Event));

        var response = await client.SendAsync(message, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            foreach (var envelope in data)
            {
                envelope.IsDelivered = true;
                _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
                    topicName, EventSubresource, envelope);
            }

            return;
        }
        
        foreach (var envelope in data)
        {
            envelope.DeliveryAttempt++;
            _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
                topicName, EventSubresource, envelope);
        }
    }

    private async Task HandleSubscriptionValidationRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string topicName, string topicId, string subscriptionName,
        HttpRequestMessage message, CancellationToken cancellationToken)
    {
        var validationEvent = EventGridValidationEvent.New(topicId);
        message.Content = JsonContent.Create(new [] { validationEvent });

        try
        {
            var response = await client.SendAsync(message, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var validationResponse =
                await response.Content.ReadFromJsonAsync<EventGridValidationEventResponse>(
                    cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode &&
                validationResponse!.ValidationResponse == validationEvent.Data?.ValidationCode)
            {
                _provider.CreateOrUpdateSubresource(subscriptionIdentifier, resourceGroupIdentifier, subscriptionName,
                    topicName, ValidatedSubscriptionSubresource,
                    new ValidatedEventSubscription(topicName, subscriptionName));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(EventGridEventDeliveryBackgroundService),
                nameof(HandleSubscriptionValidationRequest),
                $"Error while handling subscription validation request: {ex.Message}");
            throw;
        }
    }
}
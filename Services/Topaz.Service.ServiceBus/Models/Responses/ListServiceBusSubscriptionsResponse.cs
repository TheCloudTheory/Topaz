using System.Text.Json;
using Topaz.Service.ServiceBus.Models;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models.Responses;

internal sealed class ListServiceBusSubscriptionsResponse
{
    public ServiceBusSubscriptionResource[] Value { get; init; } = [];

    public static ListServiceBusSubscriptionsResponse From(ServiceBusSubscriptionResource[] subscriptions) =>
        new() { Value = subscriptions };

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

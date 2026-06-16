using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models.Responses;

internal sealed class ListServiceBusAuthorizationRulesResponse
{
    public ServiceBusAuthorizationRuleResource[] Value { get; init; } = [];

    public static ListServiceBusAuthorizationRulesResponse From(ServiceBusAuthorizationRuleResource[] rules) =>
        new() { Value = rules };

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

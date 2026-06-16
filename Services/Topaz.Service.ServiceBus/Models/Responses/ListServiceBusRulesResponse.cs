using System.Text.Json;
using Topaz.Service.ServiceBus.Models;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models.Responses;

internal sealed class ListServiceBusRulesResponse
{
    public ServiceBusRuleResource[] Value { get; init; } = [];

    public static ListServiceBusRulesResponse From(ServiceBusRuleResource[] rules) =>
        new() { Value = rules };

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

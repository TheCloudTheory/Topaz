namespace Topaz.Service.ServiceBus.Models.Requests;

internal sealed class CreateOrUpdateServiceBusAuthorizationRuleRequestProperties
{
    public List<string>? Rights { get; init; }
}

internal sealed class CreateOrUpdateServiceBusAuthorizationRuleRequest
{
    public CreateOrUpdateServiceBusAuthorizationRuleRequestProperties? Properties { get; init; }
}

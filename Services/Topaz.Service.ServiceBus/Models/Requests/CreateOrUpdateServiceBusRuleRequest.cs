using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

/// <summary>
/// The ARM envelope for a rule create-or-update. The control plane receives
/// <c>{"properties":{"filterType":…}}</c>, so the filter lives one level down — deserializing the body
/// straight into <see cref="ServiceBusRuleResourceProperties"/> binds nothing and silently falls back to
/// that type's defaults.
/// </summary>
public sealed class CreateOrUpdateServiceBusRuleRequest
{
    public ServiceBusRuleResourceProperties? Properties { get; init; } = new();

    public static CreateOrUpdateServiceBusRuleRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusRuleRequest
        {
            Properties = resource.Properties as ServiceBusRuleResourceProperties
        };
    }
}

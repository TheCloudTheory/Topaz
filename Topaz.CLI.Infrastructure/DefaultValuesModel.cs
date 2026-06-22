using System.Text.Json;
using Topaz.Shared;

namespace Topaz.CLI.Infrastructure;

public sealed class DefaultValuesModel
{
    public string? SubscriptionId { get; set; }
    public string? ResourceGroup { get; set; }
    public string? Location { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    public DefaultValuesModel UpdateWith(DefaultValuesModel newDefaults)
    {
        return new DefaultValuesModel
        {
            SubscriptionId = newDefaults.SubscriptionId ?? SubscriptionId,
            ResourceGroup = newDefaults.ResourceGroup ?? ResourceGroup,
            Location = newDefaults.Location ?? Location
        };
    }
}
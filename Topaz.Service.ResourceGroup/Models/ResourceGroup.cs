using System.Text.Json;
using Topaz.Service.Shared;

namespace Topaz.Service.ResourceGroup.Models;

public record class ResourceGroup
{
    public string Id => $"/subscriptions/{SubscriptionId}/resourceGroups/{Name}";
    public PropertiesData Properties => new();
    public string Name { get; set; }
    public string SubscriptionId { get; set; }
    public string Location { get; set; }

    public ResourceGroup(string name, string subscriptionId, string location)
    {
        Name = name;
        SubscriptionId = subscriptionId;
        Location = location;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }

    public record class PropertiesData
    {
        public static string ProvisioningState => "Succeeded";
    }
}

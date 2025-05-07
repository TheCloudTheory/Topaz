using System.Text.Json;

namespace Azure.Local.Service.ResourceGroup.Models;

public record class ResourceGroup(string Name, string Location)
{
    public string Id => $"/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/{Name}";

    public static PropertiesData Properties => new();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public record class PropertiesData
    {
        public static string ProvisioningState => "Succeeded";
    }
}

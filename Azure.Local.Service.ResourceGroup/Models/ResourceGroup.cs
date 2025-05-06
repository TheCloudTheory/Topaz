using System.Text.Json;

namespace Azure.Local.Service.ResourceGroup.Models;

public record class ResourceGroup(string Name, string Location)
{
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

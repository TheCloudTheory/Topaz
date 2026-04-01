using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed class ExportTemplateResult
{
    public required JsonNode Template { get; init; }
    public object? Error { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}

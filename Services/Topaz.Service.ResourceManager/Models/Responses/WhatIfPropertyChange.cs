using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed record WhatIfPropertyChange
{
    public string Path { get; init; } = null!;
    public string PropertyChangeType { get; init; } = null!;
    public JsonNode? Before { get; init; }
    public JsonNode? After { get; init; }

    public static WhatIfPropertyChange Create(string path, WhatIfPropertyChangeType changeType, JsonNode? before, JsonNode? after) =>
        new()
        {
            Path = path,
            PropertyChangeType = changeType.ToString(),
            Before = before,
            After = after
        };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public enum WhatIfPropertyChangeType
{
    Create,
    Delete,
    Modify,
    Array
}

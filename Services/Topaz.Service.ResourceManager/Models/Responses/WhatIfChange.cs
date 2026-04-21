using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed record WhatIfChange
{
    public string ResourceId { get; init; } = null!;
    public string ChangeType { get; init; } = null!;
    public JsonNode? Before { get; init; }
    public JsonNode? After { get; init; }
    public IReadOnlyList<WhatIfPropertyChange> Delta { get; init; } = [];

    public static WhatIfChange ForCreate(string resourceId, JsonNode after) =>
        new()
        {
            ResourceId = resourceId,
            ChangeType = nameof(WhatIfChangeType.Create),
            Before = null,
            After = after,
            Delta = []
        };

    public static WhatIfChange ForDelete(string resourceId, JsonNode before) =>
        new()
        {
            ResourceId = resourceId,
            ChangeType = nameof(WhatIfChangeType.Delete),
            Before = before,
            After = null,
            Delta = []
        };

    public static WhatIfChange ForNoChange(string resourceId, JsonNode current) =>
        new()
        {
            ResourceId = resourceId,
            ChangeType = nameof(WhatIfChangeType.NoChange),
            Before = current,
            After = current,
            Delta = []
        };

    public static WhatIfChange ForModify(string resourceId, JsonNode before, JsonNode after, IReadOnlyList<WhatIfPropertyChange> delta) =>
        new()
        {
            ResourceId = resourceId,
            ChangeType = nameof(WhatIfChangeType.Modify),
            Before = before,
            After = after,
            Delta = delta
        };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public enum WhatIfChangeType
{
    Create,
    Delete,
    Modify,
    NoChange,
    Unsupported
}

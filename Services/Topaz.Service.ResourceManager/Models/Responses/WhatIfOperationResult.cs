using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed record WhatIfOperationResult
{
    public string Status { get; init; } = "Succeeded";
    public WhatIfOperationResultProperties Properties { get; init; } = new();

    public static WhatIfOperationResult From(IReadOnlyList<WhatIfChange> changes) =>
        new() { Properties = new WhatIfOperationResultProperties { Changes = changes } };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public sealed record WhatIfOperationResultProperties
{
    public IReadOnlyList<WhatIfChange> Changes { get; init; } = [];
}

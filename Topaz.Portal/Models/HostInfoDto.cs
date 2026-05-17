using System.Text.Json.Serialization;

namespace Topaz.Portal.Models;

public sealed record HostInfoDto(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("workingDirectory")] string WorkingDirectory
);

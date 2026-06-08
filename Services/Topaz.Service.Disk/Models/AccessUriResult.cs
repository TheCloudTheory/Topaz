using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Disk.Models;

public sealed class AccessUriResult
{
    public string AccessSAS { get; init; } = string.Empty;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

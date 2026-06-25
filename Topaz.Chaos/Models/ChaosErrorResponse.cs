using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Chaos.Models;

internal sealed class ChaosErrorResponse
{
    public required ChaosErrorDetail Error { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    internal sealed class ChaosErrorDetail
    {
        public required string Code { get; init; }
        public required string Message { get; init; }
    }
}

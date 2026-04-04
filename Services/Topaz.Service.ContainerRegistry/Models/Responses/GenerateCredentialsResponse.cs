using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Models.Responses;

internal sealed class GenerateCredentialsResponse
{
    public string? Username { get; set; }
    public TokenPasswordEntry[]? Passwords { get; set; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    internal sealed class TokenPasswordEntry
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public DateTimeOffset? Expiry { get; set; }
    }
}

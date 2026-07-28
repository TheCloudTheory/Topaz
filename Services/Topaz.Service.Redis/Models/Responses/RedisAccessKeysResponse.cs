using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis.Models.Responses;

internal sealed class RedisAccessKeysResponse(string primaryKey, string secondaryKey) : TopazApiModel
{
    public string PrimaryKey { get; } = primaryKey;
    public string SecondaryKey { get; } = secondaryKey;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

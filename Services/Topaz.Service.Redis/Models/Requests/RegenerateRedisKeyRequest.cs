namespace Topaz.Service.Redis.Models.Requests;

internal sealed class RegenerateRedisKeyRequest
{
    public string? KeyType { get; init; }
}

using Topaz.Service.Redis.Models;
using Topaz.Service.Shared;

namespace Topaz.Service.Redis;

internal sealed class RedisAccessKeyStore : TopazApiModel
{
    public List<RedisAccessKey> Keys { get; set; } = [];

    public static RedisAccessKeyStore Generate(string storeName)
    {
        return new RedisAccessKeyStore
        {
            Keys =
            [
                RedisAccessKey.Create("Primary", "Primary"),
                RedisAccessKey.Create("Secondary", "Secondary")
            ]
        };
    }
}
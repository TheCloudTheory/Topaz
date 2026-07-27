using Topaz.Service.Shared;

namespace Topaz.Service.Redis.Models;

internal sealed class RedisAccessKey : TopazApiModel
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }

    public static RedisAccessKey Create(string id, string name)
    {
        var value = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(44));
        return new RedisAccessKey
        {
            Id = id,
            Name = name,
            Value = value
        };
    }
}
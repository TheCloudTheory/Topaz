using System.Text;
using Topaz.Service.Redis.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis;

internal sealed class RedisDataPlane(RedisServiceControlPlane controlPlane, ITopazLogger logger)
{
    public static RedisDataPlane New(RedisServiceControlPlane controlPlane, ITopazLogger logger) => new(controlPlane, logger);
    private readonly RedisResourceProvider _provider = new(logger);

    public DataPlaneOperationResult Set(byte[] key, byte[] value, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var valueStr = Encoding.UTF8.GetString(value);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Set), $"SET {keyStr} {valueStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult(instance.Result, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, keyStr);
        File.WriteAllText(filePath, valueStr);
        
        return new DataPlaneOperationResult(OperationResult.Success);
    }

    public DataPlaneOperationResult<string> Get(byte[] key, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Set), $"GET {keyStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string>(instance.Result, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, keyStr);

        if (!File.Exists(filePath))
        {
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, $"Key '{keyStr}' not found", "KeyNotFound");
        }
        
        var valueStr = File.ReadAllText(filePath);
        return new DataPlaneOperationResult<string>(OperationResult.Success, valueStr);
    }

    public DataPlaneOperationResult<string> Append(byte[] key, byte[] value, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var valueStr = Encoding.UTF8.GetString(value);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Set), $"SET {keyStr} {valueStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string>(instance.Result, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, keyStr);
        
        if(!File.Exists(filePath))
        {
            File.WriteAllText(filePath, valueStr);
            return new DataPlaneOperationResult<string>(OperationResult.Success, valueStr);
        }

        var currentValue = File.ReadAllText(filePath);
        var newValue = currentValue + valueStr;
        File.WriteAllText(filePath, newValue);

        return new DataPlaneOperationResult<string>(OperationResult.Success, newValue);
    }
}
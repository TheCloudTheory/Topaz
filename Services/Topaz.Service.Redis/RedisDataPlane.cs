using System.Collections.Concurrent;
using System.Text;
using Topaz.Service.Redis.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis;

internal sealed class RedisDataPlane(RedisServiceControlPlane controlPlane, ITopazLogger logger)
{
    public static RedisDataPlane New(RedisServiceControlPlane controlPlane, ITopazLogger logger) => new(controlPlane, logger);
    
    private readonly RedisResourceProvider _provider = new(logger);

    /// <summary>
    /// A concurrent dictionary that manages expiration state for keys in the Redis data plane.
    /// This dictionary maps a key (as a string) to its associated <see cref="CancellationTokenSource"/>,
    /// enabling the management of delayed tasks for key expiration.
    /// 
    /// Keys are added or updated when expiration is set, and removed when deletion or expiration
    /// operations reset the expiration state.
    /// </summary>
    private readonly ConcurrentDictionary<string, KeyExpirationEnvelope> _expirations = new();

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
        
        // SET command resets EXPIRE so if there's a key that is supposed to be expired,
        // we must remove it from the dictionary
        if (_expirations.TryRemove(filePath, out var envelope))
        {
            envelope.Cts.Cancel();
        }
        
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
        logger.LogDebug(nameof(RedisDataPlane), nameof(Append), $"APPEND {keyStr} {valueStr} for Redis instance: {cache.Name}");
        
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

    public DataPlaneOperationResult<int> Delete(byte[] key, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Append), $"DEL {keyStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, keyStr);
        if (!File.Exists(filePath))
        {
            return new DataPlaneOperationResult<int>(OperationResult.Success, 0);
        }
        
        File.Delete(filePath);
        
        // DELETE command resets EXPIRE, so if there's a key that is supposed to be expired,
        // we must remove it from the dictionary
        if (_expirations.TryRemove(filePath, out var envelope))
        {
            envelope.Cts.Cancel();
        }
        
        return new DataPlaneOperationResult<int>(OperationResult.Success, 1);

    }

    public DataPlaneOperationResult<int> Exists(byte[] key, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Append), $"EXISTS {keyStr} for Redis instance: {cache.Name}");

        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }

        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, keyStr);
        
        return !File.Exists(filePath)
            ? new DataPlaneOperationResult<int>(OperationResult.Success, 0)
            : new DataPlaneOperationResult<int>(OperationResult.Success, 1);
    }

    public DataPlaneOperationResult<int> Expire(byte[] key, byte[] expireTime, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var expireTimeStr = Encoding.UTF8.GetString(expireTime);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Append), $"EXPIRE {keyStr} {expireTimeStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, keyStr);
        
        if(!File.Exists(filePath))
        {
            return new DataPlaneOperationResult<int>(OperationResult.Success, 0);
        }
        
        var timeToExpiry = TimeSpan.FromSeconds(int.Parse(expireTimeStr));
        var cts = new CancellationTokenSource();
        
        _ = _expirations.AddOrUpdate(filePath, new KeyExpirationEnvelope(cts, DateTimeOffset.Now), (_, old) =>
        {
            old.Cts.Cancel();
            old.Cts.Dispose();
            
            return old;
        });
        
        _ = Task.Delay(timeToExpiry, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            Delete(key, cache);
            _expirations.TryRemove(filePath, out _);
        }, TaskScheduler.Default);

        return new DataPlaneOperationResult<int>(OperationResult.Success, 1);
    }

    public DataPlaneOperationResult<int> Ttl(byte[] key, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Append), $"TTL {keyStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, keyStr);

        if (!File.Exists(filePath))
        {
            return new DataPlaneOperationResult<int>(OperationResult.Success, -2);
        }
        
        var expiring = _expirations.TryGetValue(filePath, out var envelope);
        return new DataPlaneOperationResult<int>(OperationResult.Success, expiring ? (int)envelope!.ExpirationSetDate.Subtract(DateTimeOffset.UtcNow).TotalSeconds : -1);
    }

    private record KeyExpirationEnvelope(CancellationTokenSource Cts, DateTimeOffset ExpirationSetDate);

    public DataPlaneOperationResult<string[]> Keys(byte[] pattern, RedisResource cache)
    {
        var patternStr = Encoding.UTF8.GetString(pattern);
        logger.LogDebug(nameof(RedisDataPlane), nameof(Set), $"KEYS {patternStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string[]>(instance.Result, null, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var allData = Directory.GetFiles(mainPath, patternStr);
        
        return new DataPlaneOperationResult<string[]>(OperationResult.Success, [.. allData.Select(File.ReadAllText)]);
    }

    public DataPlaneOperationResult<int> HSet(byte[] key, byte[] field, byte[] value, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var fieldStr = Encoding.UTF8.GetString(field);
        var valueStr = Encoding.UTF8.GetString(value);

        logger.LogDebug(nameof(RedisDataPlane), nameof(HSet), $"HSET {keyStr} {fieldStr} {valueStr} for Redis instance: {cache.Name}");

        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, $"{keyStr}_{fieldStr}");
        
        File.WriteAllText(filePath, valueStr);

        return new DataPlaneOperationResult<int>(OperationResult.Success, 1);
    }

    public DataPlaneOperationResult<string> HGet(byte[] key, byte[] field, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var fieldStr = Encoding.UTF8.GetString(field);
        
        logger.LogDebug(nameof(RedisDataPlane), nameof(HGet), $"HGET {keyStr} {fieldStr} for Redis instance: {cache.Name}");

        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string>(instance.Result, null, instance.Reason, instance.Code);
        }
        
        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, $"{keyStr}_{fieldStr}");
        if (!File.Exists(filePath))
        {
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null, "Key not found", "KeyNotFound");
        }

        var fileContent = File.ReadAllText(filePath);
        return new DataPlaneOperationResult<string>(OperationResult.Success, fileContent);
    }

    public DataPlaneOperationResult<int> HmSet(byte[] key, List<KeyValuePair<byte[], byte[]>> pairs,
        RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        
        logger.LogDebug(nameof(RedisDataPlane), nameof(HmSet), $"HMSET {keyStr}, {pairs.Count} values for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);

        foreach (var pair in pairs)
        {
            var valueKeyStr = Encoding.UTF8.GetString(pair.Key);
            var valueStr = Encoding.UTF8.GetString(pair.Value);
            var filePath = Path.Combine(mainPath, $"{keyStr}_{valueKeyStr}");
            
            File.WriteAllText(filePath, valueStr);
        }

        return new DataPlaneOperationResult<int>(OperationResult.Success, 1);
    }

    public DataPlaneOperationResult<string[]> HGetAll(byte[] key, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        
        logger.LogDebug(nameof(RedisDataPlane), nameof(HGetAll), $"HGETALL {keyStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string[]>(instance.Result, [], instance.Reason, instance.Code);
        }
        
        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var files = Directory.EnumerateFiles(mainPath, $"{keyStr}_*");
        var result = new List<string>();

        foreach (var file in files)
        {
            var fi = new FileInfo(file);
            var segments = fi.Name.Split("_");
            var pairKey = segments[^1];
            var value = File.ReadAllText(file);
            
            result.Add(pairKey);
            result.Add(value);
        }
        
        return new DataPlaneOperationResult<string[]>(OperationResult.Success, [.. result]);
    }

    public DataPlaneOperationResult<int> HDel(byte[] key, byte[] hashField, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var hashFieldStr = Encoding.UTF8.GetString(hashField);
        logger.LogDebug(nameof(RedisDataPlane), nameof(HDel), $"HDEL {keyStr} {hashFieldStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, $"{keyStr}_{hashFieldStr}");
        if (!File.Exists(filePath))
        {
            return new DataPlaneOperationResult<int>(OperationResult.Success, 0);
        }
        
        File.Delete(filePath);
        
        return new DataPlaneOperationResult<int>(OperationResult.Success, 1);
    }

    public DataPlaneOperationResult<int> ArrayInsert(byte[] key, byte[] value, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var valueStr = Encoding.UTF8.GetString(value);
        logger.LogDebug(nameof(RedisDataPlane), nameof(ArrayInsert), $"ARINSERT {keyStr} {valueStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var array = Directory.EnumerateFiles(mainPath, $"{keyStr}_[*").ToArray();
        if (array.Length == 0)
        {
            File.WriteAllText(Path.Combine(mainPath, $"{keyStr}_[0]"), valueStr);
            return new DataPlaneOperationResult<int>(OperationResult.Success, 0);
        }
        
        var newIndex = array.Length;
        File.WriteAllText(Path.Combine(mainPath, $"{keyStr}_[{newIndex}]"), valueStr);
        return new DataPlaneOperationResult<int>(OperationResult.Success, newIndex);
    }

    public DataPlaneOperationResult<string> ArrayGet(byte[] key, byte[] index, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var indexStr = Encoding.UTF8.GetString(index);
        logger.LogDebug(nameof(RedisDataPlane), nameof(ArrayGet),
            $"ARGET {keyStr} {indexStr} for Redis instance: {cache.Name}");

        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string>(instance.Result, null, instance.Reason, instance.Code);
        }

        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, $"{keyStr}_[{indexStr}]");

        return !File.Exists(filePath)
            ? new DataPlaneOperationResult<string>(OperationResult.NotFound, null)
            : new DataPlaneOperationResult<string>(OperationResult.Success, File.ReadAllText(filePath));
    }

    public DataPlaneOperationResult<int> LPush(byte[] key, byte[] value, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var valueStr = Encoding.UTF8.GetString(value);
        logger.LogDebug(nameof(RedisDataPlane), nameof(LPush), $"LPUSH {keyStr} {valueStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var files = Directory.EnumerateFiles(mainPath, $"{keyStr}_[*").ToArray();
        if (files.Length == 0)
        {
            File.WriteAllText(Path.Combine(mainPath, $"{keyStr}_[1]"), valueStr);
            return new DataPlaneOperationResult<int>(OperationResult.Success, 1);
        }
        
        // LPUSH pushes a new element at the beginning
        if (files.Length == 1)
        {
            var destination = Path.Combine(mainPath, $"{keyStr}_b[2]");
            
            File.WriteAllText(destination, null);
            File.Replace(files[0], destination, null);
        }
        
        for (var i = 1; i < files.Length; i++)
        {
            var destination = Path.Combine(mainPath, $"{keyStr}_b[{i+1}]");
            
            File.WriteAllText(destination, null);
            File.Replace(files[i], destination, null);
        }
        
        File.WriteAllText(Path.Combine(mainPath, $"{keyStr}_b[1]"), valueStr);
        var newFiles = Directory.EnumerateFiles(mainPath, $"{keyStr}_b[*").ToArray();
        
        for (var i = 0; i < newFiles.Length; i++)
        {
            var destination = Path.Combine(mainPath, $"{keyStr}_[{i+1}]");
            
            File.WriteAllText(destination, null);
            File.Replace(newFiles[i], destination, null);
        }
        
        return new DataPlaneOperationResult<int>(OperationResult.Success, files.Length+1);
    }

    public DataPlaneOperationResult<string> LIndex(byte[] key, byte[] index, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var indexStr = Encoding.UTF8.GetString(index);
        logger.LogDebug(nameof(RedisDataPlane), nameof(LIndex),
            $"LINDEX {keyStr} {indexStr} for Redis instance: {cache.Name}");

        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string>(instance.Result, null, instance.Reason, instance.Code);
        }

        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var filePath = Path.Combine(mainPath, $"{keyStr}_[{indexStr}]");

        return !File.Exists(filePath)
            ? new DataPlaneOperationResult<string>(OperationResult.NotFound, null)
            : new DataPlaneOperationResult<string>(OperationResult.Success, File.ReadAllText(filePath));
    }

    public DataPlaneOperationResult<string> LPop(byte[] key, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        logger.LogDebug(nameof(RedisDataPlane), nameof(LPop),
            $"LPOP {keyStr} for Redis instance: {cache.Name}");

        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string>(instance.Result, null, instance.Reason, instance.Code);
        }

        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var files = Directory.EnumerateFiles(mainPath, $"{keyStr}_[*").ToArray();
        if (files.Length == 0)
        {
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null);
        }
        
        var first = File.ReadAllText(files[0]);
        
        // Reorganize all the existing elements
        File.Delete(files[0]);

        if (files.Length <= 1)
        {
            return new DataPlaneOperationResult<string>(OperationResult.Success, first);
        }
        
        for (var i = 1; i < files.Length; i++)
        {
            var destination = Path.Combine(mainPath, $"{keyStr}_b[{i+1}]");
            
            File.WriteAllText(destination, null);
            File.Replace(files[i], destination, null);
        }
        
        var newFiles = Directory.EnumerateFiles(mainPath, $"{keyStr}_b[*").ToArray();
        
        for (var i = 0; i < newFiles.Length; i++)
        {
            var destination = Path.Combine(mainPath, $"{keyStr}_[{i+1}]");
            
            File.WriteAllText(destination, null);
            File.Replace(newFiles[i], destination, null);
        }

        return new DataPlaneOperationResult<string>(OperationResult.Success, first);
    }

    public DataPlaneOperationResult<int> RPush(byte[] key, byte[] value, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        var valueStr = Encoding.UTF8.GetString(value);
        logger.LogDebug(nameof(RedisDataPlane), nameof(LPush), $"RPUSH {keyStr} {valueStr} for Redis instance: {cache.Name}");
        
        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<int>(instance.Result, 0, instance.Reason, instance.Code);
        }
        
        var mainPath = _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var files = Directory.EnumerateFiles(mainPath, $"{keyStr}_[*").ToArray();
        if (files.Length == 0)
        {
            File.WriteAllText(Path.Combine(mainPath, $"{keyStr}_[1]"), valueStr);
            return new DataPlaneOperationResult<int>(OperationResult.Success, 1);
        }
        
        File.WriteAllText(Path.Combine(mainPath, $"{keyStr}_[{files.Length+1}]"), valueStr);
        return new DataPlaneOperationResult<int>(OperationResult.Success, files.Length+1);
    }

    public DataPlaneOperationResult<string> RPop(byte[] key, RedisResource cache)
    {
        var keyStr = Encoding.UTF8.GetString(key);
        logger.LogDebug(nameof(RedisDataPlane), nameof(LPop),
            $"RPOP {keyStr} for Redis instance: {cache.Name}");

        var instance = controlPlane.Get(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        if (instance.Result != OperationResult.Success)
        {
            return new DataPlaneOperationResult<string>(instance.Result, null, instance.Reason, instance.Code);
        }

        var mainPath =
            _provider.GetServiceInstanceDataPath(cache.GetSubscription(), cache.GetResourceGroup(), cache.Name);
        var files = Directory.EnumerateFiles(mainPath, $"{keyStr}_[*").ToArray();
        if (files.Length == 0)
        {
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null);
        }

        string? content;
        if (files.Length == 1)
        {
            content = File.ReadAllText(files[0]);
            File.Delete(files[0]);
            return new DataPlaneOperationResult<string>(OperationResult.Success, content);
        }

        var last = files[^1];
        content = File.ReadAllText(last);
        File.Delete(last);

        return new DataPlaneOperationResult<string>(OperationResult.Success, content);
    }
}
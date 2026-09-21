using System.Text;
using Topaz.Service.Redis.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis;

internal sealed class Resp2ProtocolHandler(RedisServiceControlPlane controlPlane, ITopazLogger logger)
{
    private readonly RedisDataPlane _dataPlane = RedisDataPlane.New(controlPlane, logger);
    private RedisResource? _cache;

    public byte[] Handle(byte[] receiveBuffer, int noOfBytes)
    {
        var data = receiveBuffer.AsSpan(0, noOfBytes);
        var responses = new List<byte[]>();
        var pos = 0;

        // RESP2 sends commands as arrays of bulk strings:
        // AUTH password -> *2\r\n$4\r\nAUTH\r\n$6\r\npassword\r\n
        // Parsing must follow the declared lengths rather than scanning for
        // the next CRLF, since a bulk string value can itself contain "\r\n"
        // or start with '$' / '*'.
        while (pos < data.Length)
        {
            var commandName = ParseNextCommand(data, ref pos, out var commandParameters);
            if (commandName is null) break;

            responses.Add(ParseCommand(commandName, commandParameters));
        }

        var response = responses.SelectMany(r => r).ToArray();
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle),
            $"Sending response: {Encoding.UTF8.GetString(response)}");

        return response;
    }

    private static string? ParseNextCommand(ReadOnlySpan<byte> data, ref int pos, out List<byte[]> parameters)
    {
        var newlineBytes = "\r\n"u8;
        parameters = [];

        if (data[pos] != (byte)'*') return null;

        var headerEnd = data[pos..].IndexOf(newlineBytes);
        if (headerEnd < 0) return null;

        var noOfParameters = int.Parse(Encoding.ASCII.GetString(data.Slice(pos + 1, headerEnd - 1)));
        pos += headerEnd + newlineBytes.Length;

        string? commandName = null;

        for (var i = 0; i < noOfParameters; i++)
        {
            if (data[pos] != (byte)'$') return null;

            var lengthEnd = data[pos..].IndexOf(newlineBytes);
            if (lengthEnd < 0) return null;

            var length = int.Parse(Encoding.ASCII.GetString(data.Slice(pos + 1, lengthEnd - 1)));
            pos += lengthEnd + newlineBytes.Length;

            var value = data.Slice(pos, length).ToArray();
            pos += length + newlineBytes.Length;

            if (i == 0)
            {
                commandName = Encoding.UTF8.GetString(value);
            }
            else
            {
                parameters.Add(value);
            }
        }

        return commandName;
    }

    private byte[] ParseCommand(string commandName, List<byte[]> commandParameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(ParseCommand), $"Parsing command: {commandName}");

        try
        {
            switch (commandName)
            {
                case "AUTH":
                    return HandleAuthCommand(commandParameters);
                case "HELLO":
                    return HandleHelloCommand(commandParameters);
                case "CLIENT":
                    return HandleClientCommand(commandParameters);
                case "CONFIG":
                    return HandleConfigCommand(commandParameters);
                case "SENTINEL":
                    return HandleSentinelCommand(commandParameters);
                case "INFO":
                    return HandleInfoCommand(commandParameters);
                case "CLUSTER":
                    return HandleClusterCommand(commandParameters);
                case "GET":
                    return HandleGetCommand(commandParameters);
                case "ECHO":
                    return HandleEchoCommand(commandParameters);
                case "PING":
                    return HandlePingCommand(commandParameters);
                case "SET":
                    return HandleSetCommand(commandParameters);
                case "APPEND":
                    return HandleAppendCommand(commandParameters);
                case "DEL":
                    return HandleDeleteCommand(commandParameters);
                case "EXISTS":
                    return HandleExistsCommand(commandParameters);
                case "EXPIRE":
                    return HandleExpireCommand(commandParameters);
                case "TTL":
                    return HandleTtlCommand(commandParameters);
                case "KEYS":
                    return HandleKeysCommand(commandParameters);
                case "HSET":
                    return HandleHSetCommand(commandParameters);
                case "HGET":
                    return HandleHGetCommand(commandParameters);
                case "HMSET":
                    return HandleHmSetCommand(commandParameters);
                case "HGETALL":
                    return HandleHGetAllCommand(commandParameters);
                case "HDEL":
                    return HandleHDelCommand(commandParameters);
                case "ARINSERT":
                    return HandleArrayInsert(commandParameters);
                case "ARGET":
                    return HandleArrayGet(commandParameters);
                case "LPUSH":
                    return HandleLPush(commandParameters);
                case "LINDEX":
                    return HandleLIndex(commandParameters);
                case "LPOP":
                    return HandleLPop(commandParameters);
                case "RPUSH":
                    return HandleRPush(commandParameters);
                case "RPOP":
                    return HandleRPop(commandParameters);
                case "LRANGE":
                    return HandleLRange(commandParameters);
                case "SADD":
                    return HandleSAdd(commandParameters);
                case "SPOP":
                    return HandleSPop(commandParameters);
                default:
                    return $"ERR unknown subcommand or wrong number of arguments for '{commandName}'".AsSimpleError();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(Resp2ProtocolHandler), nameof(ParseCommand), $"Error parsing command: {ex.Message} {ex.StackTrace}");
            return $"ERR internal server error".AsSimpleError();
        }
    }

    private byte[] HandleSPop(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleRPop), $"Received SPOP command.");

        var key = parameters[0];
        var result = _dataPlane.SPop(key, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.AsBulkString();
    }

    private byte[] HandleSAdd(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received SADD command.");

        var key = parameters[0];
        var pairs = new List<byte[]>();

        for (var i = 1; i < parameters.Count; i++)
        {
            var next = parameters[i];
            pairs.Add(next);
        }

        var result = _dataPlane.SAdd(key, pairs, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleLRange(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleLRange), $"Received RPOP command.");

        var key = parameters[0];
        var start = parameters[1];
        var stop = parameters[2];
        var result = _dataPlane.LRange(key, start, stop, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return new[] { Array.Empty<byte>() }.AsRespArray();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.Select(k => k.AsBulkString()).ToArray().AsRespArray();
    }

    private byte[] HandleRPop(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleRPop), $"Received RPOP command.");

        var key = parameters[0];
        var result = _dataPlane.RPop(key, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.AsBulkString();
    }

    private byte[] HandleRPush(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleRPush), $"Received RPUSH command.");

        var key = parameters[0];
        var value = parameters[1];

        var result = _dataPlane.RPush(key, value, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleLPop(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleLPop), $"Received LPOP command.");

        var key = parameters[0];
        var result = _dataPlane.LPop(key, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.AsBulkString();
    }

    private byte[] HandleLIndex(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleLIndex), $"Received LINDEX command.");

        var key = parameters[0];
        var index = parameters[1];

        var result = _dataPlane.LIndex(key, index, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.AsBulkString();
    }

    private byte[] HandleLPush(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleLPush), $"Received LPUSH command.");

        var key = parameters[0];
        var value = parameters[1];

        var result = _dataPlane.LPush(key, value, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleArrayGet(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received ARGET command.");

        var key = parameters[0];
        var index = parameters[1];

        var result = _dataPlane.ArrayGet(key, index, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.AsBulkString();
    }

    private byte[] HandleArrayInsert(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received ARINSERT command.");

        var key = parameters[0];
        var value = parameters[1];

        var result = _dataPlane.ArrayInsert(key, value, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleHDelCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received HGETALL command.");

        var key = parameters[0];
        var hashField = parameters[1];

        var result = _dataPlane.HDel(key, hashField, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleHGetAllCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received HGETALL command.");

        var key = parameters[0];
        var result = _dataPlane.HGetAll(key, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.Select(k => k.AsBulkString()).ToArray().AsRespArray();
    }

    private byte[] HandleHmSetCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received HMSET command.");

        var key = parameters[0];
        var pairs = new List<KeyValuePair<byte[], byte[]>>();

        for (var i = 1; i < parameters.Count; i++)
        {
            var next = parameters[i + 1];
            pairs.Add(new KeyValuePair<byte[], byte[]>(parameters[i], next));
        }

        var result = _dataPlane.HmSet(key, pairs, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : "OK".AsSimpleString();
    }

    private byte[] HandleHGetCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received HGET command.");

        var key = parameters[0];
        var field = parameters[1];

        var result = _dataPlane.HGet(key, field, _cache!);

        if (result.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.AsBulkString();
    }

    private byte[] HandleHSetCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHSetCommand), $"Received HSET command.");

        var key = parameters[0];
        var field = parameters[1];
        var value = parameters[2];

        var result = _dataPlane.HSet(key, field, value, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleKeysCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleKeysCommand), $"Received KEYS command.");

        var pattern = parameters[0];
        var result = _dataPlane.Keys(pattern, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.Select(k => k.AsBulkString()).ToArray().AsRespArray();
    }

    private byte[] HandleTtlCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleTtlCommand), $"Received TTL command.");

        var key = parameters[0];
        var result = _dataPlane.Ttl(key, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleExpireCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleExpireCommand), $"Received EXPIRE command.");

        var key = parameters[0];
        var expireTime = parameters[1];
        var result = _dataPlane.Expire(key, expireTime, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : "OK".AsSimpleString();
    }

    private byte[] HandleExistsCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleExistsCommand), $"Received EXISTS command.");

        var key = parameters[0];
        var result = _dataPlane.Exists(key, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleDeleteCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleDeleteCommand), $"Received DEL command.");

        var key = parameters[0];
        var result = _dataPlane.Delete(key, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleAppendCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleAppendCommand), $"Received APPEND command.");

        var key = parameters[0];
        var value = parameters[1];
        var result = _dataPlane.Append(key, value, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource!.Length.ToString().AsInteger();
    }

    private byte[] HandleSetCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSetCommand), $"Received SET command.");

        var key = parameters[0];
        var value = parameters[1];
        var result = _dataPlane.Set(key, value, _cache!);

        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : "OK".AsSimpleString();
    }

    private byte[] HandlePingCommand(List<byte[]> parameters)
    {
        return parameters.Count > 0 ? parameters[0].AsBulkString() : "PONG".AsSimpleString();
    }

    private byte[] HandleEchoCommand(List<byte[]> parameters)
    {
        return parameters[0].AsBulkString();
    }

    private byte[] HandleGetCommand(List<byte[]> parameters)
    {
        var key = parameters[0];
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSetCommand), $"Received GET command.");

        var value = _dataPlane.Get(key, _cache!);
        if (value.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return value.Result != OperationResult.Success
            ? $"ERR error '{value.Reason}' ({value.Code})".AsSimpleError()
            : value.Resource!.AsBulkString();
    }

    private byte[] HandleClusterCommand(List<byte[]> parameters)
    {
        var subcommand = Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSentinelCommand),
            $"Received sentinel subcommand: {subcommand}");

        switch (subcommand)
        {
            case "SLOTS":
                return HandleClusterSlotsCommand();
            case "NODES":
                return HandleClusterNodesCommand();
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleClusterNodesCommand()
    {
        return "".AsBulkString();
    }

    private byte[] HandleClusterSlotsCommand()
    {
        return "".AsBulkString();
    }

    private byte[] HandleInfoCommand(List<byte[]> parameters)
    {
        var infoStr = parameters.Count > 0 ? Encoding.UTF8.GetString(parameters[0]) : "";
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received info command: {infoStr}");

        switch (infoStr)
        {
            case "replication":
                return "role:master\r\nconnected_slaves:0\r\n".AsBulkString();
            case "server":
                return "Topaz".AsBulkString();
            default:
                return "role:master\r\nconnected_slaves:0\r\n".AsBulkString();
        }
    }

    private byte[] HandleSentinelCommand(List<byte[]> parameters)
    {
        var subcommand = Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSentinelCommand),
            $"Received sentinel subcommand: {subcommand}");

        switch (subcommand)
        {
            case "MASTERS":
                return HandleSentinelMastersCommand();
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleSentinelMastersCommand()
    {
        return Array.Empty<byte[]>().AsRespArray();
    }

    private byte[] HandleConfigCommand(List<byte[]> parameters)
    {
        var subcommand = Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleConfigCommand),
            $"Received config subcommand: {subcommand}");

        switch (subcommand)
        {
            case "GET":
                return HandleConfigGetCommand(parameters);
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleConfigGetCommand(List<byte[]> parameters)
    {
        var configKey = Encoding.UTF8.GetString(parameters[1]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received client config key: {configKey}");

        switch (configKey)
        {
            case "replica-read-only":
                var replica = "replica-read-only".AsBulkString();
                var replicaValue = "no".AsBulkString();

                return new[] { replica, replicaValue }.AsRespArray();
            case "databases":
                var databases = "databases".AsBulkString();
                var databasesValue = "16".AsBulkString();

                return new[] { databases, databasesValue }.AsRespArray();
            default:
                return Array.Empty<byte[]>().AsRespArray();
        }
    }

    private byte[] HandleClientCommand(List<byte[]> parameters)
    {
        var subcommand = Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleClientCommand),
            $"Received client subcommand: {subcommand}");

        switch (subcommand)
        {
            case "SETNAME":
                return HandleClientSetNameCommand(parameters);
            case "SETINFO":
                return HandleClientSetInfoCommand(parameters);
            case "ID":
                return HandleClientIdCommand();
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleClientIdCommand()
    {
        return "1".AsInteger();
    }

    private byte[] HandleClientSetInfoCommand(List<byte[]> parameters)
    {
        var value = Encoding.UTF8.GetString(parameters[1]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleClientSetInfoCommand),
            $"Received client info key: {value}");

        return "OK".AsSimpleString();
    }

    private byte[] HandleClientSetNameCommand(List<byte[]> parameters)
    {
        var value = Encoding.UTF8.GetString(parameters[1]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleClientSetNameCommand),
            $"Received client info key: {value}");

        return "OK".AsSimpleString();
    }

    private byte[] HandleHelloCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHelloCommand),
            $"Received protocol version: {Encoding.UTF8.GetString(parameters[0])}");

        var server = "server".AsBulkString();
        var topaz = "topaz".AsBulkString();
        var version = "version".AsBulkString();
        var versionValue = "1.0".AsBulkString();

        return new[] { server, topaz, version, versionValue }.AsRespMap();
    }

    private byte[] HandleAuthCommand(List<byte[]> parameters)
    {
        var key = Encoding.UTF8.GetString(parameters[0]);
        var cache = controlPlane.GetByKey(key);
        if (cache.Result != OperationResult.Success)
        {
            logger.LogError(nameof(Resp2ProtocolHandler), nameof(HandleAuthCommand),
                $"Failed to authenticate with key: {key}");
            return "-ERR Authentication failed".AsBulkString();
        }

        _cache = cache.Resource!;
        return "OK".AsSimpleString();
    }
}

internal static class Resp2ProtocolHandlerExtensions
{
    public static byte[] AsBulkString(this string str)
    {
        return Encoding.UTF8.GetBytes($"${str.Length}\r\n{str}\r\n");
    }

    public static byte[] AsBulkString(this byte[] bytes)
    {
        var prefix = Encoding.ASCII.GetBytes($"${bytes.Length}\r\n");
        var suffix = "\r\n"u8.ToArray();

        return [.. prefix, .. bytes, .. suffix];
    }

    public static byte[] AsSimpleError(this string str) =>
        Encoding.ASCII.GetBytes($"-{str}\r\n");

    public static byte[] AsSimpleString(this string str) =>
        Encoding.ASCII.GetBytes($"+{str}\r\n");

    public static byte[] AsNilBulkString() => [.. "$-1\r\n"u8];

    public static byte[] AsInteger(this string str) => Encoding.ASCII.GetBytes($":{str}\r\n");

    extension(byte[][] elements)
    {
        public byte[] AsRespMap()
        {
            // %N counts key-value pairs, not total elements.
            var prefix = Encoding.ASCII.GetBytes($"%{elements.Length / 2}\r\n");
            return [.. prefix, .. elements.SelectMany(e => e)];
        }

        public byte[] AsRespArray()
        {
            var prefix = Encoding.ASCII.GetBytes($"*{elements.Length}\r\n");
            return [.. prefix, .. elements.SelectMany(e => e)];
        }
    }
}
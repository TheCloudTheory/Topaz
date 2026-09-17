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
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Sending response: {Encoding.UTF8.GetString(response)}");
        
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
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{commandName}'".AsSimpleError();
        }
    }

    private byte[] HandleExistsCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSetCommand), $"Received EXISTS command.");
        
        var key = parameters[0];
        var result = _dataPlane.Exists(key, _cache!);
        
        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleDeleteCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSetCommand), $"Received DEL command.");
        
        var key = parameters[0];
        var result = _dataPlane.Delete(key, _cache!);
        
        return result.Result != OperationResult.Success
            ? $"ERR error '{result.Reason}' ({result.Code})".AsSimpleError()
            : result.Resource.ToString().AsInteger();
    }

    private byte[] HandleAppendCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSetCommand), $"Received APPEND command.");

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
        if(value.Result == OperationResult.NotFound)
        {
            return Resp2ProtocolHandlerExtensions.AsNilBulkString();
        }

        return value.Result != OperationResult.Success
            ? $"ERR error '{value.Reason}' ({value.Code})".AsSimpleError()
            : value.Resource!.AsBulkString();
    }

    private byte[] HandleClusterCommand(List<byte[]> parameters)
    {
        var subcommand =  Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSentinelCommand), $"Received sentinel subcommand: {subcommand}");
        
        switch(subcommand)
        {
            case "SLOTS":
                return HandleClusterSlotsCommand(parameters);
            case "NODES":
                return HandleClusterNodesCommand(parameters);
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleClusterNodesCommand(List<byte[]> parameters)
    {
        return "".AsBulkString();
    }

    private byte[] HandleClusterSlotsCommand(List<byte[]> parameters)
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
        var subcommand =  Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleSentinelCommand), $"Received sentinel subcommand: {subcommand}");
        
        switch(subcommand)
        {
            case "MASTERS":
                return HandleSentinelMastersCommand(parameters);
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleSentinelMastersCommand(List<byte[]> parameters)
    {
        return Array.Empty<byte[]>().AsRespArray();
    }

    private byte[] HandleConfigCommand(List<byte[]> parameters)
    {
        var subcommand =  Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleConfigCommand), $"Received config subcommand: {subcommand}");
        
        switch(subcommand)
        {
            case "GET":
                return HandleConfigGetCommand(parameters);
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleConfigGetCommand(List<byte[]> parameters)
    {
        var configKey =  Encoding.UTF8.GetString(parameters[1]);
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
        var subcommand =  Encoding.UTF8.GetString(parameters[0]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleClientCommand), $"Received client subcommand: {subcommand}");
        
        switch(subcommand)
        {
            case "SETNAME":
                return HandleClientSetNameCommand(parameters);
            case "SETINFO":
                return HandleClientSetInfoCommand(parameters);
            case "ID":
                return HandleClientIdCommand(parameters);
            default:
                return $"ERR unknown subcommand or wrong number of arguments for '{subcommand}'".AsSimpleError();
        }
    }

    private byte[] HandleClientIdCommand(List<byte[]> parameters)
    {
        return "1".AsInteger();
    }

    private byte[] HandleClientSetInfoCommand(List<byte[]> parameters)
    {
        var value =  Encoding.UTF8.GetString(parameters[1]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleClientSetInfoCommand), $"Received client info key: {value}");
                
        return "OK".AsSimpleString();
    }

    private byte[] HandleClientSetNameCommand(List<byte[]> parameters)
    {
        var value =  Encoding.UTF8.GetString(parameters[1]);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleClientSetNameCommand), $"Received client info key: {value}");
                
        return "OK".AsSimpleString();
    }

    private byte[] HandleHelloCommand(List<byte[]> parameters)
    {
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(HandleHelloCommand), $"Received protocol version: {Encoding.UTF8.GetString(parameters[0])}");
                
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
            logger.LogError(nameof(Resp2ProtocolHandler), nameof(HandleAuthCommand), $"Failed to authenticate with key: {key}");
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
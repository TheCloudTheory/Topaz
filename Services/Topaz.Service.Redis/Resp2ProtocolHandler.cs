using System.Text;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis;

internal sealed class Resp2ProtocolHandler(RedisServiceControlPlane controlPlane, ITopazLogger logger)
{
    private enum TokenType
    {
        NoOp,
        Array,
        ArrayNoOfElements,
        BulkString,
        SimpleString,
        Error,
        Integer,
        AuthStart,
        HelloStart,
        AuthBulkString,
        HelloBulkString,
        ClientStart,
        ClientBulkString,
        ClientSetNameStart,
        ClientSetInfoStart,
        ClientSetNameBulkString,
        ClientSetInfoKeyBulkString,
        ClientSetInfoValueBulkString,
        ClientSetInfoValueStart,
        ClientIdStart,
        ClientConfigStart,
        ClientConfigBulkString,
        ClientConfigGetStart,
        ClientConfigGetBulkString
    }

    private static readonly IDictionary<char, TokenType> SpecialTokens = new Dictionary<char, TokenType>
    {
        {'*', TokenType.Array},
        {'$', TokenType.BulkString},
        {'+', TokenType.SimpleString},
        {'-', TokenType.Error},
        {':', TokenType.Integer}
    };

    public byte[] Handle(byte[] receiveBuffer, int noOfBytes)
    {
        var newlineBytes = "\r\n"u8;
        var data = receiveBuffer.AsSpan(0, noOfBytes);
        var segments = new List<byte[]>();
        var start = 0;
        
        while (start <= data.Length)
        {
            var idx = data[start..].IndexOf(newlineBytes);
            if (idx < 0)
            {
                if (start < data.Length)
                {
                    segments.Add([.. data[start..]]);
                }
                
                break;
            }

            segments.Add([.. data.Slice(start, idx)]);
            start += idx + newlineBytes.Length;
        }

        var currentToken = TokenType.NoOp;
        var responses = new List<string>();
        foreach (var segment in segments)
        {
            var line = Encoding.UTF8.GetString(segment);
            logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received line: {line}");
            
            var firstChar = line[0];
            if (SpecialTokens.TryGetValue(firstChar, out var tokenType) && currentToken != TokenType.AuthStart &&
                currentToken != TokenType.HelloStart && currentToken != TokenType.ClientStart &&
                currentToken != TokenType.ClientSetNameStart && currentToken != TokenType.ClientSetInfoStart &&
                currentToken != TokenType.ClientSetInfoValueStart && currentToken != TokenType.ClientIdStart &&
                currentToken != TokenType.ClientConfigStart && currentToken != TokenType.ClientConfigGetStart)
            {
                currentToken = tokenType;
                continue;
            }

            if (currentToken == TokenType.AuthStart)
            {
                // In RESP2, AUTH command is followed by a bulk string,
                // so the flow looks like this:
                // AUTH
                // $XXX
                // <key>
                // We're skipping the bulk string length and moving directly to the key.
                currentToken = TokenType.AuthBulkString;
                continue;
            }
            
            if(currentToken == TokenType.HelloStart)
            {
                currentToken = TokenType.HelloBulkString;
                continue;
            }

            if (currentToken == TokenType.ClientStart)
            {
                currentToken = TokenType.ClientBulkString;
                continue;
            }
            
            if (currentToken == TokenType.ClientSetNameStart)
            {
                currentToken = TokenType.ClientSetNameBulkString;
                continue;
            }
            
            if (currentToken == TokenType.ClientSetInfoStart)
            {
                currentToken = TokenType.ClientSetInfoKeyBulkString;
                continue;
            }
            
            if (currentToken == TokenType.ClientSetInfoValueStart)
            {
                currentToken = TokenType.ClientSetInfoValueBulkString;
                continue;
            }
            
            if(currentToken == TokenType.ClientIdStart)
            {
                // Just return a fake client ID
                responses.Add("1".AsBulkString());
                currentToken = TokenType.NoOp;
                continue;
            }
            
            if(currentToken == TokenType.ClientConfigStart)
            {
                currentToken = TokenType.ClientConfigBulkString;
                continue;
            }
            
            if(currentToken == TokenType.ClientConfigGetStart)
            {
                currentToken = TokenType.ClientConfigGetBulkString;
                continue;
            }

            if(currentToken == TokenType.AuthBulkString)
            {
                var key = line;
                var cache = controlPlane.GetByKey(key);
                if (cache.Result != OperationResult.Success)
                {
                    logger.LogError(nameof(Resp2ProtocolHandler), nameof(Handle), $"Failed to authenticate with key: {key}");
                    responses.Add("-ERR Authentication failed".AsBulkString());
                    break;
                }
                
                responses.Add("+OK".AsBulkString());
                
                // Reset the current token
                currentToken = TokenType.NoOp;
                continue;
            }

            if (currentToken == TokenType.HelloBulkString)
            {
                var protocolVersion = line;
                logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received protocol version: {protocolVersion}");
                
                responses.Add(new[] {"server".AsBulkString(), "topaz".AsBulkString(), "version".AsBulkString(), "1.0".AsBulkString() }.AsRespMap());
                currentToken = TokenType.NoOp;
                continue;
            }

            if (currentToken == TokenType.ClientBulkString)
            {
                var command = line;
                if (command == "SETNAME")
                {
                    currentToken = TokenType.ClientSetNameStart;
                    continue;
                }
                
                if(command == "SETINFO")
                {
                    currentToken = TokenType.ClientSetInfoStart;
                    continue;
                }

                if (command == "ID")
                {
                    currentToken = TokenType.ClientIdStart;
                    continue;
                }
            }

            if (currentToken == TokenType.ClientSetNameBulkString)
            {
                var name = line;
                logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received client name: {name}");

                responses.Add("+OK".AsBulkString());
                currentToken = TokenType.NoOp;
                
                continue;
            }

            if (currentToken == TokenType.ClientSetInfoKeyBulkString)
            {
                var key = line;
                logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received client info key: {key}");
                
                currentToken = TokenType.ClientSetInfoValueStart;
                continue;
            }
            
            if (currentToken == TokenType.ClientSetInfoValueBulkString)
            {
                var value = line;
                logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received client info key: {value}");
                
                responses.Add("+OK".AsBulkString());
                currentToken = TokenType.NoOp;
                continue;
            }
            
            if(currentToken == TokenType.ClientConfigBulkString)
            {
                var configOp = line;
                logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received client config op: {configOp}");

                if (configOp == "GET")
                {
                    currentToken = TokenType.ClientConfigGetStart;
                    continue;
                }
                
                continue;
            }
            
            if(currentToken == TokenType.ClientConfigGetBulkString)
            {
                var configKey = line;
                logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received client config key: {configKey}");

                switch (configKey)
                {
                    case "replica-read-only":
                        responses.Add(new[] { "replica-read-only".AsBulkString(), "no".AsBulkString() }.AsRespArray());
                        break;
                    case "databases":
                        responses.Add(new[] { "databases".AsBulkString(), "16".AsBulkString() }.AsRespArray());
                        break;
                }

                currentToken = TokenType.NoOp;
                continue;
            }

            if (currentToken == TokenType.Array)
            {
                currentToken = TokenType.ArrayNoOfElements;
                continue;
            }

            if (currentToken == TokenType.BulkString)
            {
                var str = line;
                if (str == "AUTH")
                {
                    currentToken = TokenType.AuthStart;
                    continue;
                }

                if (str == "HELLO")
                {
                    currentToken = TokenType.HelloStart;
                    continue;
                }

                if (str == "CLIENT")
                {
                    currentToken = TokenType.ClientStart;
                    continue;
                }
                
                if (str == "CONFIG")
                {
                    currentToken = TokenType.ClientConfigStart;
                }
            }
        }

        var response = string.Join("", responses);
        logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Sending response: {response}");
        
        return Encoding.UTF8.GetBytes(response);
    }
}

internal static class Resp2ProtocolHandlerExtensions
{
    public static string AsBulkString(this string str)
    {
        return $"${str.Length}\r\n{str}\r\n";
    }
    
    extension(string[] lines)
    {
        public string AsRespMap()
        {
            var items = lines.Length;
            var prefix = $"%{items}";
        
            return $"{prefix}\r\n{string.Join("", lines)}";
        }

        public string AsRespArray() => $"*{lines.Length}\r\n{string.Join("", lines)}";
    }
}
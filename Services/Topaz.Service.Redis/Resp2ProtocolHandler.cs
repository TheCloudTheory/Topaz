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
        HelloBulkString
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
            if (SpecialTokens.TryGetValue(firstChar, out var tokenType) && currentToken != TokenType.AuthStart && currentToken != TokenType.HelloStart)
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
            }

            if (currentToken == TokenType.HelloBulkString)
            {
                var protocolVersion = line;
                logger.LogDebug(nameof(Resp2ProtocolHandler), nameof(Handle), $"Received protocol version: {protocolVersion}");
                
                responses.Add(new[] {"server".AsBulkString(), "topaz".AsBulkString(), "version".AsBulkString(), "1.0".AsBulkString() }.AsRespMap());
                currentToken = TokenType.NoOp;
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
    
    public static string AsRespMap(this string[] lines)
    {
        var items = lines.Length;
        var prefix = $"%{items}";
        
        return $"{prefix}\r\n{string.Join("", lines)}";
    }
}
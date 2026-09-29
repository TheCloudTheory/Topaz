---
sidebar_position: 6
description: How Topaz receives, parses, and responds to Redis RESP2 commands, including supported command groups and protocol limitations.
keywords: [topaz redis, RESP2, redis protocol, redis emulator, redis commands]
---

# Redis RESP2 protocol

Topaz accepts Redis commands over TCP using the Redis Serialization Protocol (RESP). Its Redis data-plane listener passes received bytes to a RESP2 handler, which parses commands, invokes the Redis data plane, and writes the encoded response to the socket.

## Request format

Commands are sent as RESP arrays of bulk strings. The first bulk string is the command name; the remaining bulk strings are its arguments. For example, `AUTH cache-key` is encoded as:

```text
*2\r\n$4\r\nAUTH\r\n$9\r\ncache-key\r\n
```

The parser uses each bulk string's declared byte length to find its end. This allows argument values to contain carriage returns, line feeds, or bytes that resemble RESP markers such as `$` and `*`.

The handler can parse several complete commands from one socket receive and concatenates their responses in the same order. It does not preserve an incomplete command between receives. TCP does not preserve client write boundaries, so a frame can arrive across multiple receives; this handler does not reassemble it.

## Responses

The handler encodes responses using RESP simple strings, errors, integers, bulk strings, nil bulk strings, and arrays. For example, `PING` without an argument returns a simple string, while a missing key read returns a nil bulk string. `HELLO` is an exception: it returns a RESP3 map.

Unknown commands return an error response. Command names are matched in uppercase by the handler.

## Authentication and resource selection

Send `AUTH` with a cache key before data commands. Topaz looks up that key through the Redis control plane and uses the resulting Redis resource for subsequent data-plane operations on the connection. A failed lookup returns an authentication error. The handler does not parse the optional username form of Redis `AUTH`.

## Supported command groups

The RESP2 handler dispatches these commands:

| Group | Commands |
|---|---|
| Connection and server | `AUTH`, `HELLO`, `CLIENT SETNAME`, `CLIENT SETINFO`, `CLIENT ID`, `CONFIG GET`, `INFO`, `SENTINEL MASTERS`, `CLUSTER SLOTS`, `CLUSTER NODES`, `PING`, `ECHO` |
| Strings and keys | `GET`, `SET`, `APPEND`, `DEL`, `EXISTS`, `EXPIRE`, `TTL`, `KEYS`, `INCR`, `DECR`, `FLUSHDB` |
| Hashes | `HSET`, `HGET`, `HMSET`, `HGETALL`, `HDEL` |
| Lists | `LPUSH`, `RPUSH`, `LPOP`, `RPOP`, `LINDEX`, `LRANGE` |
| Sets | `SADD`, `SPOP`, `SMEMBERS`, `SREM` |
| Redis arrays (Redis 8.8+) | `ARINSERT`, `ARGET` |

This list describes commands dispatched by the protocol handler; it does not imply full Redis command or option compatibility. Some commands accept only the arguments implemented by their handler.

## Protocol boundaries

- The listener uses one socket receive buffer of 4096 bytes and does not retain partial RESP frames between receives. TCP may split a frame across receives, which this handler does not reassemble.
- The handler expects commands in array-of-bulk-strings form; inline command input is not parsed.
- `HELLO` does not negotiate protocol behavior. It returns a RESP3 map, even though the handler is named for RESP2.
- `ARINSERT` and `ARGET` are Redis array commands introduced in Redis 8.8.0 ([`ARINSERT`](https://redis.io/docs/latest/commands/arinsert/), [`ARGET`](https://redis.io/docs/latest/commands/arget/)). Topaz's `ARINSERT` handler reads one value and does not process additional values in the same request.

See [Supported services](../supported-services.md) for Redis Cache's current support status.
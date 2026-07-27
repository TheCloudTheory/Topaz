using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis;

internal sealed class RedisResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<RedisService>(logger);
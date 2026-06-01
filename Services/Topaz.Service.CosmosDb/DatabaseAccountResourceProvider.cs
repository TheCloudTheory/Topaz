using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb;

internal sealed class DatabaseAccountResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<CosmosDbService>(logger);

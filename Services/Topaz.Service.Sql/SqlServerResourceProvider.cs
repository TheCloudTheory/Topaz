using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Sql;

internal sealed class SqlServerResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<SqlService>(logger);

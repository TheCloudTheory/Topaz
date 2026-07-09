using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics;

internal sealed class WorkspaceResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<LogAnalyticsService>(logger);

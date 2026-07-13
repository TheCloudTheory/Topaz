using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights;

internal sealed class ApplicationInsightsResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<InsightsService>(logger);

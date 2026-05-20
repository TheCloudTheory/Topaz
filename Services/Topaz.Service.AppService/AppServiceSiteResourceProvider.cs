using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService;

internal sealed class AppServiceSiteResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<AppServiceSiteService>(logger) { }

using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService;

internal sealed class AppServicePlanResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<AppServicePlanService>(logger) { }

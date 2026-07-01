using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration;

internal sealed class AppConfigurationResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<AppConfigurationService>(logger);

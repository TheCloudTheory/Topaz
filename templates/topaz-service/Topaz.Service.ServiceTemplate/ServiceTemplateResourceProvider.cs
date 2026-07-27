using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceTemplate;

internal sealed class ServiceTemplateResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ServiceTemplateService>(logger);

using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement;

internal sealed class ApiManagementResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<ApiManagementService>(logger);
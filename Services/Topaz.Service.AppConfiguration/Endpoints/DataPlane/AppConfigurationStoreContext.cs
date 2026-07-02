using Topaz.Service.Shared.Domain;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

internal sealed record AppConfigurationStoreContext(
    string StoreName,
    SubscriptionIdentifier Sub,
    ResourceGroupIdentifier Rg);

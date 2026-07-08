using Topaz.EventPipeline;
using Topaz.Service.Authorization;
using Topaz.Shared;

namespace Topaz.Service.Storage.Security;

/// <summary>
/// Validates Bearer tokens for Azure Storage data-plane requests, performing a full RBAC check
/// via <see cref="AzureAuthorizationAdapter.PrincipalHasPermissions"/> (same as Key Vault RBAC mode).
/// </summary>
internal sealed class StorageDataPlaneAuthorizationChecker(Pipeline eventPipeline, ITopazLogger logger) : DataPlaneAuthorizationChecker(eventPipeline, logger)
{
    /// <summary>
    /// WWW-Authenticate challenge returned when no valid Authorization header is present.
    /// The Azure Storage SDK uses this to discover the token endpoint and retry with a token.
    /// </summary>
    public static string WwwAuthenticateChallenge =>
        $"Bearer authorization=\"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/{GlobalSettings.DefaultTenantId}\"," +
        $" resource=\"https://storage.azure.com\"";

    /// <summary>
    /// WWW-Authenticate challenge emitted when a request reaches a private container with no
    /// authentication at all (no Authorization header, no SAS). Matches the header real Azure
    /// returns in this case so that SDK clients and applications behave identically.
    /// </summary>
    public static string PrivateContainerWwwAuthenticateChallenge =>
        $"Bearer authorization_uri=\"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/{GlobalSettings.DefaultTenantId}/oauth2/authorize\"," +
        $" resource_id=\"https://storage.azure.com\"";
}

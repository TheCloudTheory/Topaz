using System.Net;
using System.Text;
using Azure.ResourceManager.Storage.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Topaz.Dns;
using Topaz.EventPipeline;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Security;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints;

internal class DataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger)
{
    protected readonly StorageResourceProvider StorageResourceProvider = new(logger);
    private readonly BlobStorageSecurityProvider _securityProvider = new(eventPipeline, logger);
    
    protected bool TryGetStorageAccount(IHeaderDictionary headers, out StorageAccountResource? storageAccount, out string? accountName)
    {
        logger.LogDebug(nameof(DataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Trying to get storage account.");

        if (!headers.TryGetValue("Host", out var host))
        {
            logger.LogError(nameof(DataPlaneEndpointBase), nameof(TryGetStorageAccount), "{0}", "`Host` header not found - it's required for storage account creation.");

            storageAccount = null;
            accountName = null;
            return false;
        }

        var pathParts = host.ToString().Split('.');
        accountName = pathParts[0];

        logger.LogDebug(nameof(DataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "About to check if storage account '{0}' exists.", accountName);

        var identifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, accountName!);
        if (identifiers != null)
        {
            storageAccount = StorageResourceProvider.GetAs<StorageAccountResource>(
                SubscriptionIdentifier.From(identifiers.Value.subscription),
                ResourceGroupIdentifier.From(identifiers.Value.resourceGroup), accountName);
            return true;
        }

        // Secondary endpoint fallback: strip "-secondary" suffix and resolve the primary account.
        // Only RA-GRS/RAGZRS accounts expose a readable secondary endpoint.
        const string secondarySuffix = "-secondary";
        if (accountName!.EndsWith(secondarySuffix, StringComparison.OrdinalIgnoreCase))
        {
            var primaryName = accountName[..^secondarySuffix.Length];
            var primaryIdentifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, primaryName);
            if (primaryIdentifiers != null)
            {
                var primaryAccount = StorageResourceProvider.GetAs<StorageAccountResource>(
                    SubscriptionIdentifier.From(primaryIdentifiers.Value.subscription),
                    ResourceGroupIdentifier.From(primaryIdentifiers.Value.resourceGroup), primaryName);
                if (primaryAccount != null && IsRaGrsAccount(primaryAccount))
                {
                    storageAccount = primaryAccount;
                    return true;
                }
            }
        }

        storageAccount = null;
        logger.LogDebug(nameof(DataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Storage account '{0}' doesn't exists.", accountName);

        return false;
    }

    protected static bool IsRaGrsAccount(StorageAccountResource storageAccount)
    {
        var skuName = storageAccount.Sku?.Name;
        return string.Equals(skuName, StorageSkuName.StandardRagrs.ToString(), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(skuName, StorageSkuName.StandardRagzrs.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    
    protected bool IsRequestAuthorized(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string[] requiredPermissions,
        HttpContext context,
        HttpResponseMessage response)
    {
        var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget
                        ?? context.Request.Path.Value
                        ?? string.Empty;
        var queryIndex = rawTarget.IndexOf('?');
        var rawPath = queryIndex >= 0 ? rawTarget[..queryIndex] : rawTarget;

        var authResult = _securityProvider.RequestIsAuthorized(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, context.Request.Headers, requiredPermissions, context.Request.Method,
            rawPath, context.Request.QueryString, context.Connection.RemoteIpAddress);

        if (!authResult.IsAuthorized)
        {
            if (authResult.ErrorCode is "AuthorizationPermissionMismatch" or "AuthorizationSourceIPMismatch")
            {
                // Service SAS permission mismatch or source IP denied
                var error = authResult.ErrorCode == "AuthorizationSourceIPMismatch"
                    ? StorageErrorResponse.AuthorizationSourceIPMismatch()
                    : StorageErrorResponse.AuthorizationPermissionMismatch();
                response.StatusCode = HttpStatusCode.Forbidden;
                response.Content = new StringContent(error.ToXml(), Encoding.UTF8, "application/xml");
            }
            else if (authResult.ErrorCode == "NoAuthenticationInformation")
            {
                // No credentials at all on a private container — use the challenge format
                // that real Azure emits so SDK clients and applications behave identically.
                var error = StorageErrorResponse.AuthenticationFailed();
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.Headers.TryAddWithoutValidation("WWW-Authenticate", StorageDataPlaneAuthorizationChecker.PrivateContainerWwwAuthenticateChallenge);
                response.Content = new StringContent(error.ToXml(), Encoding.UTF8, "application/xml");
            }
            else
            {
                // Authentication failure (bad signature, expired token, etc.)
                var error = StorageErrorResponse.AuthenticationFailed();
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.Headers.TryAddWithoutValidation("WWW-Authenticate", StorageDataPlaneAuthorizationChecker.WwwAuthenticateChallenge);
                response.Content = new StringContent(error.ToXml(), Encoding.UTF8, "application/xml");
            }
        }

        return authResult.IsAuthorized;
    }
}
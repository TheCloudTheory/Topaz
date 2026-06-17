using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.DataPlane;

/// <summary>
/// Resolves an incoming Cosmos DB data-plane HTTP request to the corresponding
/// <see cref="DatabaseAccountResource"/> persisted by the control plane.
///
/// The account name is extracted from the first DNS label of the request's Host header
/// (e.g. <c>myaccount</c> from <c>myaccount.documents.topaz.local.dev:8895</c>) and
/// looked up in the global DNS registry.
/// </summary>
internal sealed class CosmosDbDataPlane(DatabaseAccountResourceProvider provider, ITopazLogger logger)
{
    /// <summary>
    /// Resolves the Cosmos DB account associated with the incoming request.
    /// Returns <c>null</c> when the host header cannot be mapped to a known account.
    /// </summary>
    internal DatabaseAccountResource? ResolveAccount(HttpContext context)
    {
        var accountName = context.Request.Host.Host.Split('.')[0];

        var identifiers = GlobalDnsEntries.GetEntry(CosmosDbService.UniqueName, accountName);
        if (identifiers == null)
        {
            logger.LogDebug(nameof(CosmosDbDataPlane), nameof(ResolveAccount),
                "No DNS entry found for Cosmos DB account '{0}'", accountName);
            return null;
        }

        var sub = SubscriptionIdentifier.From(identifiers.Value.subscription);
        var rg = ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!);

        var resource = provider.GetAs<DatabaseAccountResource>(sub, rg, accountName);
        if (resource == null)
        {
            logger.LogDebug(nameof(CosmosDbDataPlane), nameof(ResolveAccount),
                "Cosmos DB account '{0}' found in DNS but not persisted on disk", accountName);
        }

        return resource;
    }
}

using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.CosmosDb;

internal sealed record CosmosDbAccountContext(
    DatabaseAccountResource Account,
    SubscriptionIdentifier Sub,
    ResourceGroupIdentifier Rg);

using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.CosmosDb;

internal interface ICosmosDbControlPlane
{
    ControlPlaneOperationResult<DatabaseAccountResource[]> ListBySubscription(SubscriptionIdentifier subscriptionIdentifier);
}

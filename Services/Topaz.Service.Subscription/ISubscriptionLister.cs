using Topaz.Service.Shared;

namespace Topaz.Service.Subscription;

public interface ISubscriptionLister
{
    ControlPlaneOperationResult<Models.Subscription[]> List();
}

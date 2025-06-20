using Topaz.Service.ServiceBus.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus;

internal sealed class ServiceBusServiceControlPlane(ResourceProvider provider, ITopazLogger logger)
{
    public (OperationResult result, ServiceBusNamespaceResource? resource) CreateOrUpdateNamespace(SubscriptionIdentifier subscription, ResourceGroupIdentifier resourceGroup, string location,
        string name)
    {
        var existingNamespace = provider.GetAs<ServiceBusNamespaceResource>(name);
        if (existingNamespace == null)
        {
            var properties = new ServiceBusNamespaceResourceProperties
            {
                CreatedAt = DateTimeOffset.Now
            };

            var resource = new ServiceBusNamespaceResource(subscription, resourceGroup, location, name, properties);
            provider.CreateOrUpdate(name, resource);
            
            return (OperationResult.Created, resource);
        }

        return (OperationResult.Updated, existingNamespace);
    }
}
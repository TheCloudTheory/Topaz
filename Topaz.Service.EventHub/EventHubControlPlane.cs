using System.Text.Json;
using Topaz.Service.EventHub.Models;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class EventHubControlPlane(ResourceProvider provider, ITopazLogger logger)
{
    public object CreateNamespace(string name, string resourceGroup, string location, string subscriptionId)
    {
        var model = new Namespace()
        {
            Name = name,
            ResourceGroup = resourceGroup,
            Location = location,
            SubscriptionId = subscriptionId
        };

        provider.Create(name, model);

        return model;
    }

    public Models.EventHub Create(string name, string namespaceName)
    {
        var @namespace = this.GetNamespace(namespaceName);
        var model = Models.EventHub.New(name, namespaceName, @namespace.ResourceGroup!, @namespace.SubscriptionId!);
        
        var hub = provider.CreateEventHub(namespaceName, name, model);
        
        return hub;
    }

    public Namespace GetNamespace(string namespaceName)
    {
        var rawData = provider.Get(namespaceName);
        var @namespace = JsonSerializer.Deserialize<Namespace>(rawData, GlobalSettings.JsonOptions);
        
        return @namespace!;
    }

    public Models.EventHub CreateUpdateEventHub(string namespaceName, string name, Stream input)
    {
        logger.LogDebug($"Executing {nameof(CreateUpdateEventHub)}: {namespaceName} {name}");
        
        if (provider.EventHubExists(namespaceName, name))
        {
            throw new NotImplementedException();
        }
        else
        {
            using var sr = new StreamReader(input);
            
            var rawContent = sr.ReadToEnd();

            logger.LogDebug($"Executing {nameof(CreateUpdateEventHub)}: Processing {rawContent}.");
            
            var @namespace = this.GetNamespace(namespaceName);
            var hub = provider.CreateEventHub(namespaceName, name, Models.EventHub.New(name, namespaceName, @namespace.ResourceGroup, @namespace.Location));
            
            return hub;
        }
    }

    public void Delete(string name, string namespaceName)
    {
        logger.LogDebug($"Executing {nameof(Delete)}: {name} {namespaceName}");

        if (provider.EventHubExists(namespaceName, name) == false)
        {
            // TODO: Return proper error
            return;
        }

        provider.DeleteEventHub(name, namespaceName);
    }
}
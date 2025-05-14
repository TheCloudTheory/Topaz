using System.Text.Json;
using Topaz.Service.EventHub.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class EventHubControlPlane(ResourceProvider provider, ILogger logger)
{
    private readonly ResourceProvider provider = provider;
    private readonly ILogger logger = logger;

    public object CreateNamespace(string name, string resourceGroup, string location, string subscriptionId)
    {
        var model = new Namespace()
        {
            Name = name,
            ResourceGroup = resourceGroup,
            Location = location,
            SubscriptionId = subscriptionId
        };

        this.provider.Create(name, model);

        return model;
    }

    public Models.EventHub Create(string name, string namespaceName)
    {
        var @namespace = this.GetNamespace(namespaceName);
        var model = Models.EventHub.New(name, namespaceName, @namespace.ResourceGroup!, @namespace.SubscriptionId!);
        
        var hub = this.provider.CreateEventHub(namespaceName, name, model);
        
        return hub;
    }

    public Namespace GetNamespace(string namespaceName)
    {
        var rawData = this.provider.Get(namespaceName);
        var @namespace = JsonSerializer.Deserialize<Namespace>(rawData, GlobalSettings.JsonOptions);
        
        return @namespace!;
    }

    public Models.EventHub CreateUpdateEventHub(string namespaceName, string name, Stream input)
    {
        this.logger.LogDebug($"Executing {nameof(CreateUpdateEventHub)}: {namespaceName} {name}");
        
        if (this.provider.EventHubExists(namespaceName, name))
        {
            throw new NotImplementedException();
        }
        else
        {
            using var sr = new StreamReader(input);
            
            var rawContent = sr.ReadToEnd();

            this.logger.LogDebug($"Executing {nameof(CreateUpdateEventHub)}: Processing {rawContent}.");
            
            var @namespace = this.GetNamespace(namespaceName);
            var hub = this.provider.CreateEventHub(namespaceName, name, Models.EventHub.New(name, namespaceName, @namespace.ResourceGroup, @namespace.Location));
            
            return hub;
        }
    }

    public void Delete(string name, string namespaceName)
    {
        this.logger.LogDebug($"Executing {nameof(Delete)}: {name} {namespaceName}");

        if (this.provider.EventHubExists(namespaceName, name) == false)
        {
            // TODO: Return proper error
            return;
        }

        this.provider.DeleteEventHub(name, namespaceName);
    }
}
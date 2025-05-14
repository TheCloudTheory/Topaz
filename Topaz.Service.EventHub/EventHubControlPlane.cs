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

    public object Create(string settingsName, string settingsResourceGroup, string settingsLocation, string settingsSubscriptionId)
    {
        throw new NotSupportedException();
    }

    public Namespace GetNamespace(string namespaceName)
    {
        var rawData = this.provider.Get(namespaceName);
        var @namespace = JsonSerializer.Deserialize<Namespace>(rawData, GlobalSettings.JsonOptions);
        
        return @namespace!;
    }

    public Models.EventHub CreateUpdateEventHub(string namespaceName, string eventhubName, Stream input)
    {
        this.logger.LogDebug($"Executing {nameof(CreateUpdateEventHub)}: {namespaceName} {eventhubName}");
        
        if (this.provider.EventHubExists(namespaceName, eventhubName))
        {
            throw new NotImplementedException();
        }
        else
        {
            using var sr = new StreamReader(input);
            
            var rawContent = sr.ReadToEnd();

            this.logger.LogDebug($"Executing {nameof(CreateUpdateEventHub)}: Processing {rawContent}.");
            
            var @namespace = this.GetNamespace(namespaceName);
            var hub = this.provider.CreateEventHub(namespaceName, eventhubName, Models.EventHub.New(eventhubName, namespaceName, @namespace.ResourceGroup, @namespace.Location));
            
            return hub;
        }
    }
}
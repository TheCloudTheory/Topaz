using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class EventHubResourceProvider(ITopazLogger logger) : ResourceProviderBase<EventHubService>(logger) 
{
    private readonly ITopazLogger _topazLogger = logger;
    
    public bool EventHubExists(string namespaceName, string eventhubName)
    {
        return Directory.Exists(this.GetEventHubPath(namespaceName, eventhubName));
    }

    private string GetEventHubPath(string namespaceName, string eventhubName)
    {
        return Path.Combine(BaseEmulatorPath, EventHubService.LocalDirectoryPath, namespaceName, "hubs", eventhubName);
    }

    public void DeleteEventHub(string name, string namespaceName)
    {
        var hubPath = GetEventHubPath(namespaceName, name);
        
        _topazLogger.LogDebug(nameof(EventHubResourceProvider), nameof(DeleteEventHub), "Attempting to delete {0} directory.", hubPath);
        
        Directory.Delete(hubPath);
        
        _topazLogger.LogDebug(nameof(EventHubResourceProvider), nameof(DeleteEventHub), "Attempting to delete {0} directory - deleted!", hubPath);
    }
}
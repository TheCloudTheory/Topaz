using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

internal sealed class ResourceProvider(ITopazLogger logger) : ResourceProviderBase<EventHubService>(logger) 
{
    private readonly ITopazLogger _topazLogger = logger;
    
    public bool EventHubExists(string namespaceName, string eventhubName)
    {
        return Directory.Exists(this.GetEventHubPath(namespaceName, eventhubName));
    }

    public string GetEventHubPath(string namespaceName, string eventhubName)
    {
        return Path.Combine(BaseEmulatorPath, EventHubService.LocalDirectoryPath, namespaceName, "hubs", eventhubName);
    }

    public Models.EventHub CreateEventHub(string namespaceName, string eventhubName, Models.EventHub model)
    {
        var metadataFile = $"metadata.json";
        var hubPath = this.GetEventHubPath(namespaceName, eventhubName);
        var dataPath = Path.Combine(hubPath, "data");
        var metadataFilePath = Path.Combine(hubPath, metadataFile);
        
        this._topazLogger.LogDebug($"Attempting to create {hubPath} directory.");
        
        Directory.CreateDirectory(hubPath);
        Directory.CreateDirectory(dataPath);
        
        this._topazLogger.LogDebug($"Attempting to create {hubPath} directory - created!");
        this._topazLogger.LogDebug($"Attempting to create {metadataFilePath} file.");
        
        var content = JsonSerializer.Serialize(model, GlobalSettings.JsonOptions);
        File.WriteAllText(metadataFilePath, content);

        return model;
    }

    public void DeleteEventHub(string name, string namespaceName)
    {
        var hubPath = this.GetEventHubPath(namespaceName, name);
        
        this._topazLogger.LogDebug($"Attempting to delete {hubPath} directory.");
        
        Directory.Delete(hubPath);
        
        this._topazLogger.LogDebug($"Attempting to delete {hubPath} directory - deleted!");
    }
}
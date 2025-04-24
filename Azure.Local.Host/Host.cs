using Azure.Local.Service.Storage;
using Azure.Local.Shared;

namespace Azure.Local.Host;

public class Host
{
    public void Start()
    {
        var services = new [] { new AzureStorageService() };

        foreach(var service in services)
        {
            PrettyLogger.LogInformation($"Starting {service.Name} service...");
        }
    }
}

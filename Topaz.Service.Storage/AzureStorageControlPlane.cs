using System.Text.Json;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class AzureStorageControlPlane(ResourceProvider provider, ILogger logger)
{
    private readonly ResourceProvider provider = provider;
    private readonly ILogger logger = logger;

    public Models.StorageAccount Get(string name)
    {
        var data = this.provider.Get(name);
        var model = JsonSerializer.Deserialize<Models.StorageAccount>(data, GlobalSettings.JsonOptions);

        return model!;
    }

    public Models.StorageAccount Create(string name, string resourceGroup, string location, string subscriptionId)
    {
        if(CheckIfResourceGroupExists(resourceGroup) == false)
        {
            throw new InvalidOperationException();
        }

        var model = new Models.StorageAccount(name, resourceGroup, location, subscriptionId);

        this.provider.Create(name, model);

        return model;
    }

    private bool CheckIfResourceGroupExists(string resourceGroup)
    {
        var rp = new ResourceGroupControlPlane(new ResourceGroup.ResourceProvider(this.logger));
        var data = rp.Get(resourceGroup);

        return data != null;
    }

    internal void Delete(string storageAccountName)
    {
        this.provider.Delete(storageAccountName);
    }
}

using System.Text.Json;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models;

internal sealed record StorageAccountConnectionString
{
    [Obsolete("Only for serialization")]
    public StorageAccountConnectionString()
    {
    }

    public StorageAccountConnectionString(string storageAccountName, string accessKey)
    {
        ConnectionString = TopazResourceHelpers.GetAzureStorageConnectionString(storageAccountName, accessKey);
    }
    
    public string? ConnectionString { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptionsCli);
    }
}
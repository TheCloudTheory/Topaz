using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models.Responses.StorageAccount;

internal sealed class ListStorageAccountsResponse
{
    public StorageAccountResource[]? Value { get; init; }
    
    public static ListStorageAccountsResponse From(StorageAccountResource[]? storageAccounts)
    {
        return new ListStorageAccountsResponse
        {
            Value = storageAccounts
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
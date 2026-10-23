using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class AdlsServiceControlPlane(ITopazLogger logger)
{
    private readonly AdlsResourceProvider _provider = new(logger);

    public static AdlsServiceControlPlane New(ITopazLogger logger) => new(logger);

    /// <summary>
    /// Create a filesystem rooted at the specified location. If the filesystem already exists, the operation fails. This operation does not support conditional HTTP requests.
    /// </summary>
    public ControlPlaneOperationResult CreateFileSystem(StorageAccountResource storageAccount, string filesystem,
        IHeaderDictionary headers, string apiVersion)
    {
        logger.LogDebug(nameof(AdlsServiceControlPlane), nameof(CreateFileSystem),
            $"Creating filesystem {filesystem} on storage account {storageAccount.Name}");

        var existingFilesystem = GetFileSystem(storageAccount, filesystem, apiVersion);
        
        if (existingFilesystem.Result == OperationResult.Success)
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict, "The specified filesystem already exists.", "FilesystemAlreadyExists");
        }
        
        var properties = headers["x-ms-properties"].ToString();
        var encryptionScope = headers["x-ms-encryption-scope"].ToString();
        
        _provider.CreateFileSystem(storageAccount, filesystem, new Filesystem(properties, encryptionScope));
        
        var responseHeaders = new Dictionary<string, string>
        {
            { "Date", DateTime.UtcNow.ToString("R") },
            { "ETag", "123456789" },
            { "Last-Modified", DateTime.UtcNow.ToString("R") },
            { "x-ms-request-id", Guid.NewGuid().ToString() },
            { "x-ms-version", apiVersion },
            { "x-ms-namespace-enabled", storageAccount.Properties.IsHnsEnabled.ToString() ?? "false" },
            { "x-ms-default-encryption-scope", encryptionScope }
        };

        return new ControlPlaneOperationResult(OperationResult.Created, null, null, responseHeaders);
    }

    public ControlPlaneOperationResult GetFileSystem(StorageAccountResource storageAccount, string filesystem, string apiVersion)
    {
        logger.LogDebug(nameof(AdlsServiceControlPlane), nameof(GetFileSystem),
            $"Getting filesystem {filesystem} on storage account {storageAccount.Name}");

        var headers = new Dictionary<string, string>
        {
            { "Date", DateTime.UtcNow.ToString("R") },
            { "ETag", "123456789" },
            { "Last-Modified", DateTime.UtcNow.ToString("R") },
            { "x-ms-request-id", Guid.NewGuid().ToString() },
            { "x-ms-version", apiVersion },
            { "x-ms-properties", "" }, // To be set if there are custom user properties
            { "x-ms-namespace-enabled", storageAccount.Properties.IsHnsEnabled.ToString() ?? "false" },
            { "x-ms-default-encryption-scope", "" }, // The scope configured for the filesystem. If it uses the account’s default, that is commonly represented as $account-encryption-key; don’t assume the value is always empty.
            { "x-ms-deny-encryption-scope-override", "false" } // false unless the filesystem is configured to prohibit override
        };

        return new ControlPlaneOperationResult(OperationResult.Success, null, null, headers);
    }
}
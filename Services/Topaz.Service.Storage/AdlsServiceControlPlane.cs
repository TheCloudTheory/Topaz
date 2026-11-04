using Microsoft.AspNetCore.Http;
using System.Globalization;
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

        var existingFilesystem= GetFileSystem(storageAccount, filesystem, apiVersion);
        
        if (existingFilesystem.Result == OperationResult.Success)
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict, "The specified filesystem already exists.", "FilesystemAlreadyExists");
        }
        
        if (existingFilesystem.Result == OperationResult.Conflict)
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict, existingFilesystem.Reason, existingFilesystem.Code);
        }
        
        var properties = headers["x-ms-properties"].ToString();
        var encryptionScope = headers["x-ms-encryption-scope"].ToString();
        var filesystemMetadata = new Filesystem(filesystem, properties, encryptionScope);
        
        _provider.CreateFileSystem(storageAccount, filesystem, filesystemMetadata);
        
        var responseHeaders = new Dictionary<string, string>
        {
            { "Date", DateTime.UtcNow.ToString("R") },
            { "ETag", filesystemMetadata.ETag!.Value.ToString("H") },
            { "Last-Modified", DateTime.UtcNow.ToString("R") },
            { "x-ms-request-id", Guid.NewGuid().ToString() },
            { "x-ms-version", apiVersion },
            { "x-ms-namespace-enabled", storageAccount.Properties.IsHnsEnabled.ToString() ?? "false" },
            { "x-ms-default-encryption-scope", encryptionScope }
        };

        return new ControlPlaneOperationResult(OperationResult.Created, null, null, responseHeaders);
    }

    public ControlPlaneOperationResult<Filesystem> GetFileSystem(StorageAccountResource storageAccount, string filesystemName, string apiVersion)
    {
        logger.LogDebug(nameof(AdlsServiceControlPlane), nameof(GetFileSystem),
            $"Getting filesystem {filesystemName} on storage account {storageAccount.Name}");

        var filesystem = _provider.GetFileSystem(storageAccount, filesystemName);
        if (filesystem == null)
        {
            return new ControlPlaneOperationResult<Filesystem>(OperationResult.NotFound, null, "The specified filesystem does not exist.", "FilesystemNotFound");
        }
        
        if (filesystem.DeleteScheduled)
        {
            return new ControlPlaneOperationResult<Filesystem>(OperationResult.Conflict, null, "The specified filesystem is being deleted.", "FilesystemBeingDeleted");
        }
        
        var headers = new Dictionary<string, string>
        {
            { "Date", DateTime.UtcNow.ToString("R") },
            { "ETag", filesystem.ETag!.Value.ToString("H") },
            { "Last-Modified", filesystem.LastModified?.ToString("R", CultureInfo.InvariantCulture) ?? DateTime.UtcNow.ToString("R") },
            { "x-ms-request-id", Guid.NewGuid().ToString() },
            { "x-ms-version", apiVersion },
            { "x-ms-properties", "" }, // To be set if there are custom user properties
            { "x-ms-namespace-enabled", storageAccount.Properties.IsHnsEnabled.ToString() ?? "false" },
            { "x-ms-default-encryption-scope", "" }, // The scope configured for the filesystem. If it uses the account’s default, that is commonly represented as $account-encryption-key; don’t assume the value is always empty.
            { "x-ms-deny-encryption-scope-override", "false" } // false unless the filesystem is configured to prohibit override
        };

        return new ControlPlaneOperationResult<Filesystem>(OperationResult.Success, filesystem, null, null, headers);
    }

    public ControlPlaneOperationResult DeleteFileSystem(StorageAccountResource storageAccount, string filesystem, string apiVersion)
    {
        logger.LogDebug(nameof(AdlsServiceControlPlane), nameof(CreateFileSystem),
            $"Creating filesystem {filesystem} on storage account {storageAccount.Name}");

        var existingFilesystem= GetFileSystem(storageAccount, filesystem, apiVersion);
        if (existingFilesystem.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, existingFilesystem.Reason, existingFilesystem.Code);
        }
        
        // The filesystem may be already being deleted. If that's the case, 
        if (existingFilesystem.Result == OperationResult.Conflict)
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict, existingFilesystem.Reason, existingFilesystem.Code);
        }

        _provider.DeleteFileSystem(storageAccount, filesystem);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<Filesystem[]> ListFilesystems(StorageAccountResource storageAccount)
    {
        logger.LogDebug(nameof(AdlsServiceControlPlane), nameof(ListFilesystems),
            $"Listing filesystems on storage account {storageAccount.Name}");
        
        var filesystems = _provider.ListFilesystems(storageAccount);
        
        return new ControlPlaneOperationResult<Filesystem[]>(OperationResult.Success, filesystems);
    }

    public ControlPlaneOperationResult BeginDeletingFilesystem(StorageAccountResource storageAccount, string filesystem, string apiVersion, IHeaderDictionary requestHeaders)
    {
        logger.LogDebug(nameof(AdlsServiceControlPlane), nameof(CreateFileSystem),
            $"Creating filesystem {filesystem} on storage account {storageAccount.Name}");

        var existingFilesystem= GetFileSystem(storageAccount, filesystem, apiVersion);
        if (existingFilesystem.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, existingFilesystem.Reason, existingFilesystem.Code);
        }
        
        // The filesystem may be already being deleted. If that's the case, 
        if (existingFilesystem.Result == OperationResult.Conflict)
        {
            return new ControlPlaneOperationResult(OperationResult.Conflict, existingFilesystem.Reason, existingFilesystem.Code);
        }

        var existingFilesystemResource = existingFilesystem.Resource!;
        var lastModified = existingFilesystemResource.LastModified ?? DateTimeOffset.MinValue;
        if (DateTimeOffset.TryParse(requestHeaders["If-Modified-Since"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ifModifiedSince) && lastModified <= ifModifiedSince ||
            DateTimeOffset.TryParse(requestHeaders["If-Unmodified-Since"], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ifUnmodifiedSince) && lastModified > ifUnmodifiedSince)
        {
            return new ControlPlaneOperationResult(OperationResult.PreconditionFailed,
                "The condition specified using HTTP conditional header(s) is not met.", "ConditionNotMet");
        }

        existingFilesystemResource.MarkForDeletion();
        _provider.UpdateFilesystem(storageAccount, filesystem, existingFilesystemResource);
        
        return new ControlPlaneOperationResult(OperationResult.Success);
    }
}
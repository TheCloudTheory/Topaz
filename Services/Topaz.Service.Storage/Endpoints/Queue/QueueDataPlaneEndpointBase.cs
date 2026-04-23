using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal abstract class QueueDataPlaneEndpointBase(ITopazLogger logger)
{
    private readonly StorageResourceProvider _storageResourceProvider = new(logger);
    protected readonly ITopazLogger Logger = logger;

    protected bool TryGetStorageAccount(IHeaderDictionary headers, out StorageAccountResource? storageAccount)
    {
        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Trying to get storage account.");

        if (!headers.TryGetValue("Host", out var host))
        {
            Logger.LogError("`Host` header not found - it's required for storage account creation.");

            storageAccount = null;
            return false;
        }

        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];

        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "About to check if storage account '{0}' exists.", accountName);

        var identifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, accountName!);
        if (identifiers != null)
        {
            storageAccount = _storageResourceProvider.GetAs<StorageAccountResource>(
                SubscriptionIdentifier.From(identifiers.Value.subscription),
                ResourceGroupIdentifier.From(identifiers.Value.resourceGroup), accountName);
            return true;
        }

        storageAccount = null;
        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Storage account '{0}' doesn't exist.", accountName);

        return false;
    }

    protected string GetQueueName(string path)
    {
        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(GetQueueName),
            "Looking for queue name in {0}", path);

        var pathParts = path.TrimStart('/').Split('/');

        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(GetQueueName), "Returning: {0}", pathParts[0]);

        return pathParts[0];
    }

    protected static bool TryGetQueueNameFromPath(string queuePath, out string? queueName)
    {
        var matches = Regex.Match(queuePath, @"^/([^/?]+)", RegexOptions.Compiled);
        if (matches.Success && matches.Groups.Count > 1)
        {
            queueName = matches.Groups[1].Value;
            return true;
        }

        queueName = null;
        return false;
    }
}

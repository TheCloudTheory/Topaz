using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Exceptions;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Security;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal abstract class TableDataPlaneEndpointBase(ITopazLogger logger)
{
    private readonly StorageResourceProvider _storageResourceProvider = new(logger);
    private readonly TableStorageSecurityProvider _securityProvider = new(logger);
    protected readonly ITopazLogger Logger = logger;
    protected readonly TableServiceControlPlane ControlPlane = new(new TableResourceProvider(logger), logger);
    protected readonly TableServiceDataPlane DataPlane = new(new TableResourceProvider(logger), logger);

    protected bool TryGetStorageAccount(IHeaderDictionary headers, out StorageAccountResource? storageAccount)
    {
        Logger.LogDebug(nameof(TableDataPlaneEndpointBase), nameof(TryGetStorageAccount), "Executing {0}",
            nameof(TryGetStorageAccount));

        if (!headers.TryGetValue("Host", out var host))
        {
            Logger.LogError("`Host` header not found - it's required for storage account creation.");

            storageAccount = null;
            return false;
        }

        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];

        Logger.LogDebug(nameof(TableDataPlaneEndpointBase), nameof(TryGetStorageAccount),
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
        return false;
    }

    protected bool IsRequestAuthorized(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        IHeaderDictionary headers,
        string path,
        QueryString query)
    {
        return _securityProvider.RequestIsAuthorized(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, headers, path, query);
    }

    protected bool IsPathReferencingTable(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string tablePath,
        string storageAccountName)
    {
        Logger.LogDebug(nameof(TableDataPlaneEndpointBase), nameof(IsPathReferencingTable),
            "Executing {0}: {1} {2}", nameof(IsPathReferencingTable), tablePath, storageAccountName);

        var tableName = tablePath.Replace("/", string.Empty);
        return ControlPlane.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            tableName);
    }

    protected (string TableName, string PartitionKey, string RowKey) GetOperationDataForUpdateOperation(Match matches)
    {
        Logger.LogDebug(nameof(TableDataPlaneEndpointBase), nameof(GetOperationDataForUpdateOperation),
            "Executing {0}: {1}", nameof(GetOperationDataForUpdateOperation), matches);

        var match = matches.Value;
        var dataMatches = Regex.Match(match,
            @"^(?<tableName>\w+)\(PartitionKey='(?<partitionKey>\w+)',RowKey='(?<rowKey>\w+)'\)$");

        var tableName = dataMatches.Groups["tableName"].Value;
        var partitionKey = dataMatches.Groups["partitionKey"].Value;
        var rowKey = dataMatches.Groups["rowKey"].Value;

        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
        {
            throw new InvalidInputException();
        }

        return (TableName: tableName, PartitionKey: partitionKey, RowKey: rowKey);
    }

    protected void HandleUpdateEntityRequest(
        Stream input,
        IHeaderDictionary headers,
        Match matches,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        HttpResponseMessage response)
    {
        Logger.LogDebug(nameof(TableDataPlaneEndpointBase), nameof(HandleUpdateEntityRequest),
            "Matched the update operation.");

        var (tableName, partitionKey, rowKey) = GetOperationDataForUpdateOperation(matches);

        try
        {
            DataPlane.UpdateEntity(input, subscriptionIdentifier, resourceGroupIdentifier, tableName,
                storageAccountName, partitionKey, rowKey, headers);

            response.StatusCode = HttpStatusCode.NoContent;
        }
        catch (EntityNotFoundException)
        {
            var error = new TableErrorResponse("EntityNotFound", "Entity not found.");

            response.StatusCode = HttpStatusCode.NotFound;
            response.Headers.Add("x-ms-error-code", "EntityNotFound");
            response.Content = JsonContent.Create(error);
        }
        catch (UpdateConditionNotSatisfiedException)
        {
            var error = new TableErrorResponse("UpdateConditionNotSatisfied",
                "The update condition specified in the request was not satisfied.");

            response.StatusCode = HttpStatusCode.PreconditionFailed;
            response.Headers.Add("x-ms-error-code", "UpdateConditionNotSatisfied");
            response.Content = JsonContent.Create(error);
        }
    }
}

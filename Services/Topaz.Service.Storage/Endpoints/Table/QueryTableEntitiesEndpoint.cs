using Topaz.EventPipeline;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class QueryTableEntitiesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : TableDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    public string ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["GET /{tableName}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/entities/read"];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out var originalStorageAccountName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name, context, response))
            return;

        if (context.Request.Query.HasQueryKeyWithValue("comp", "acl"))
        {
            HandleGetAclRequest(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                context.Request.Path, response);
            return;
        }

        var potentialTableName = context.Request.Path.Value!
            .Replace("()", string.Empty)
            .Replace("/", string.Empty);

        if (IsPathReferencingTable(subscriptionIdentifier, resourceGroupIdentifier, potentialTableName,
                storageAccount.Name))
        {
            var result = DataPlane.QueryEntities(context.Request.QueryString, subscriptionIdentifier,
                resourceGroupIdentifier, potentialTableName, storageAccount.Name, originalStorageAccountName!);

            if (result.NextPartitionKey is not null)
            {
                response.Headers.Add("x-ms-continuation-NextPartitionKey", result.NextPartitionKey);
                if (result.NextRowKey is not null)
                    response.Headers.Add("x-ms-continuation-NextRowKey", result.NextRowKey);
            }

            response.Content = JsonContent.Create(new TableDataEndpointResponse(result.Entities));
            response.StatusCode = HttpStatusCode.OK;
            return;
        }

        response.StatusCode = HttpStatusCode.NotFound;
    }

    private void HandleGetAclRequest(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string path,
        HttpResponseMessage response)
    {
        Logger.LogDebug(nameof(QueryTableEntitiesEndpoint), nameof(HandleGetAclRequest), "Executing {0}.",
            nameof(HandleGetAclRequest));

        var trimmedPath = path.TrimEnd('/');
        var tableName = Path.GetFileName(trimmedPath);
        if (string.IsNullOrEmpty(tableName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        // Inline guard: CodeQL's cs/path-injection barrier (PathCheck) requires a direct boolean
        // guard on the tainted variable — wrapping the check in a method call is not recognised.
        // Path.GetFileName above strips directory separators; this guard rejects any that remain.
        if (tableName.Contains('/') || tableName.Contains('\\') || tableName.Contains(".."))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var aclsOp = ControlPlane.GetAcl(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(SignedIdentifiers));
        serializer.Serialize(sw, aclsOp.Resource!);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private sealed class TableDataEndpointResponse(object?[] values)
    {
        [UsedImplicitly] public object?[] Value { get; init; } = values;
    }
}

using Topaz.EventPipeline;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class QueryTableEntitiesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : TableDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["GET /{tableName}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/entities/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name, context))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

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
            var entities = DataPlane.QueryEntities(context.Request.QueryString, subscriptionIdentifier,
                resourceGroupIdentifier, potentialTableName, storageAccount.Name);
            response.Content = JsonContent.Create(new TableDataEndpointResponse(entities));
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

        // Path.GetFileName is called directly so static-analysis tools (e.g. CodeQL) can recognise
        // the taint as cleared here. PathGuard.ValidateName provides defence-in-depth against any
        // remaining forbidden characters such as "..".
        // TrimEnd is called on a separate variable so GetFileName receives a plain tainted identifier,
        // not a method-call expression — this helps pattern-based SAST tools match the sanitizer.
        var trimmedPath = path.TrimEnd('/');
        var tableName = Path.GetFileName(trimmedPath);
        if (string.IsNullOrEmpty(tableName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        PathGuard.ValidateName(tableName);

        var aclsOp = ControlPlane.GetAcl(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, tableName);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(SignedIdentifiers));
        serializer.Serialize(sw, aclsOp.Resource!);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private sealed class TableDataEndpointResponse(object?[] values)
    {
        public object?[] Value { get; init; } = values;
    }
}

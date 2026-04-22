using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Exceptions;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class GetTableEntityEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => [@"GET /^.*?\(PartitionKey='.*?',(%20|\s)?RowKey='.*?'\)$"];

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

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                context.Request.Headers, context.Request.Method, context.Request.Path, context.Request.QueryString))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

        var matches = Regex.Match(context.Request.Path, @"\w+\(PartitionKey='\w+',(%20|\s)?RowKey='\w+'\)$",
            RegexOptions.IgnoreCase);

        var (tableName, partitionKey, rowKey) = GetOperationDataForUpdateOperation(matches);

        try
        {
            var entityJson = DataPlane.GetEntity(subscriptionIdentifier, resourceGroupIdentifier, tableName,
                storageAccount.Name, partitionKey, rowKey);

            var entity = JsonSerializer.Deserialize<object>(entityJson, GlobalSettings.JsonOptions);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = JsonContent.Create(entity);
        }
        catch (EntityNotFoundException)
        {
            var error = new TableErrorResponse("EntityNotFound", "Entity not found.");

            response.StatusCode = HttpStatusCode.NotFound;
            response.Headers.Add("x-ms-error-code", "EntityNotFound");
            response.Content = JsonContent.Create(error);
        }
    }
}

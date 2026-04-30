using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Exceptions;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class InsertTableEntityEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["POST /{tableName}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/entities/write"];

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

        var normalizedTablePath = context.Request.Path.Value!
            .Replace("()", string.Empty)
            .Replace("/", string.Empty);

        if (IsPathReferencingTable(subscriptionIdentifier, resourceGroupIdentifier, "/" + normalizedTablePath,
                storageAccount.Name))
        {
            try
            {
                var tableName = normalizedTablePath;
                var payload = DataPlane.InsertEntity(context.Request.Body, subscriptionIdentifier,
                    resourceGroupIdentifier, tableName, storageAccount.Name);

                if (!context.Request.Headers.TryGetValue("Prefer", out var prefer) ||
                    prefer != "return-no-content")
                {
                    response.StatusCode = HttpStatusCode.Created;
                    var entity = JsonSerializer.Deserialize<object>(payload, GlobalSettings.JsonOptions);
                    response.Content = JsonContent.Create(entity);
                }

                if (prefer == "return-no-content")
                {
                    response.StatusCode = HttpStatusCode.NoContent;
                }

                return;
            }
            catch (EntityAlreadyExistsException)
            {
                var error = new TableErrorResponse("EntityAlreadyExists", "Entity already exists.");

                response.StatusCode = HttpStatusCode.Conflict;
                response.Headers.Add("x-ms-error-code", "EntityAlreadyExists");
                response.Content = JsonContent.Create(error);

                return;
            }
        }

        response.StatusCode = HttpStatusCode.NotFound;
    }
}

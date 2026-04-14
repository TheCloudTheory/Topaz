using System.Net;
using System.Net.Http.Json;
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
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Http);

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
                context.Request.Headers, context.Request.Path, context.Request.QueryString))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

        if (IsPathReferencingTable(subscriptionIdentifier, resourceGroupIdentifier, context.Request.Path,
                storageAccount.Name))
        {
            try
            {
                var tableName = context.Request.Path.Value!.Replace("/", string.Empty);
                var payload = DataPlane.InsertEntity(context.Request.Body, subscriptionIdentifier,
                    resourceGroupIdentifier, tableName, storageAccount.Name);

                if (!context.Request.Headers.TryGetValue("Prefer", out var prefer) ||
                    prefer != "return-no-content")
                {
                    response.StatusCode = HttpStatusCode.Created;
                    response.Content = JsonContent.Create(payload);
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

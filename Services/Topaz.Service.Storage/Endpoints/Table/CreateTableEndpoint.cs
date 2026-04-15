using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class CreateTableEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["POST /Tables"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/write"];

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
                context.Request.Headers, context.Request.Method, context.Request.Path, context.Request.QueryString))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

        Logger.LogDebug(nameof(CreateTableEndpoint), nameof(GetResponse), "Executing {0}.",
            nameof(CreateTableEndpoint));

        using var sr = new StreamReader(context.Request.Body);
        var rawContent = sr.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateTableRequest>(rawContent, GlobalSettings.JsonOptions);

        if (request == null || string.IsNullOrEmpty(request.TableName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var tableExists = ControlPlane.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name, request.TableName);
        if (tableExists)
        {
            var error = new TableErrorResponse("TableAlreadyExists", "Table already exists.");

            response.StatusCode = HttpStatusCode.Conflict;
            response.Headers.Add("x-ms-error-code", "TableAlreadyExists");
            response.Content = JsonContent.Create(error);

            return;
        }

        var table = ControlPlane.CreateTable(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name, request);

        if (!context.Request.Headers.TryGetValue("Prefer", out var prefer) || prefer != "return-no-content")
        {
            response.Content = JsonContent.Create(table);
            response.StatusCode = HttpStatusCode.Created;
            response.Headers.Add("Preference-Applied", "return-content");
        }

        if (prefer == "return-no-content")
        {
            response.StatusCode = HttpStatusCode.NoContent;
            response.Headers.Add("Preference-Applied", "return-no-content");
        }
    }
}

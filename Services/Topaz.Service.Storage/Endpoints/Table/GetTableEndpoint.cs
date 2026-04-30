using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class GetTableEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => [@"GET /^Tables\('.*?'\)$"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/read"];

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

        var match = Regex.Match(context.Request.Path.Value!, @"^\/Tables\('(.*?)'\)$", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var tableName = match.Groups[1].Value;

        if (!ControlPlane.CheckIfTableExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                tableName))
        {
            var error = new TableErrorResponse("TableNotFound", "The specified table does not exist.");
            response.StatusCode = HttpStatusCode.NotFound;
            response.Headers.Add("x-ms-error-code", "TableNotFound");
            response.Content = JsonContent.Create(error);
            return;
        }

        response.Content = JsonContent.Create(new TableProperties { Name = tableName });
        response.StatusCode = HttpStatusCode.OK;
    }
}

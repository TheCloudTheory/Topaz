using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Exceptions;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class DeleteTableEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => [@"DELETE /^Tables\('.*?'\)$"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/delete"];

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

        try
        {
            var matches = Regex.Match(context.Request.Path, @"^\/Tables\('.*?'\)$", RegexOptions.IgnoreCase);
            if (matches.Length == 0)
            {
                throw new Exception($"Invalid request path {context.Request.Path} for the delete operation.");
            }

            var tableName = matches.Value.Trim('/').Replace("Tables('", "").Replace("')", "");

            Logger.LogDebug(nameof(DeleteTableEndpoint), nameof(GetResponse),
                "Attempting to delete table: {0}.", tableName);
            ControlPlane.DeleteTable(subscriptionIdentifier, resourceGroupIdentifier, tableName,
                storageAccount.Name);
            Logger.LogDebug(nameof(DeleteTableEndpoint), nameof(GetResponse), "Table {0} deleted.", tableName);

            response.StatusCode = HttpStatusCode.NoContent;
        }
        catch (EntityNotFoundException)
        {
            var error = new TableErrorResponse("EntityNotFound", "Table not found.");

            response.StatusCode = HttpStatusCode.NotFound;
            response.Headers.Add("x-ms-error-code", "EntityNotFound");
            response.Content = JsonContent.Create(error);
        }
    }
}

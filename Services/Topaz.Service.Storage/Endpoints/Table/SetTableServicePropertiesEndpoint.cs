using System.Net;
using System.Web;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class SetTableServicePropertiesEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["PUT /"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/write"];

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

        ThrowIfSetPropertiesRequestIsInvalid(context.Request.QueryString);

        ControlPlane.SetTableProperties(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
            context.Request.Body);

        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
        response.StatusCode = HttpStatusCode.Accepted;
    }

    private static void ThrowIfSetPropertiesRequestIsInvalid(QueryString query)
    {
        if (!query.HasValue) throw new Exception($"QueryString '{query}' is missing.");

        var collection = HttpUtility.ParseQueryString(query.Value!);
        if (!collection.AllKeys.Contains("restype") || !collection.AllKeys.Contains("comp"))
            throw new Exception("Query string is missing required fields.");

        var restype = collection["restype"];
        var comp = collection["comp"];

        if (restype != "service") throw new Exception("Invalid value for 'restype'.");
        if (comp != "properties") throw new Exception("Invalid value for 'comp'.");
    }
}

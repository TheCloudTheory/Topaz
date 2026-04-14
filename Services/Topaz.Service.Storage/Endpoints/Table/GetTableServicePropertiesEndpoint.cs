using System.Net;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class GetTableServicePropertiesEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["GET /"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/read"];

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

        ThrowIfGetPropertiesRequestIsInvalid(context.Request.QueryString);

        var properties = ControlPlane.GetTableProperties(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name);

        using var sw = new EncodingAwareStringWriter();
        var serializer = new XmlSerializer(typeof(TableServiceProperties));
        serializer.Serialize(sw, properties);

        response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private static void ThrowIfGetPropertiesRequestIsInvalid(QueryString query)
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

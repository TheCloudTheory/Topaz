using System.Net;
using System.Text;
using System.Web;
using Azure.ResourceManager.Storage.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class GetTableServicePropertiesEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["GET /"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (context.Request.QueryString.TryGetValueForKey("comp", out var comp) && comp == "stats")
        {
            HandleGetStats(context, response);
            return;
        }

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

        ThrowIfGetPropertiesRequestIsInvalid(context.Request.QueryString);

        var propertiesXmlOp = ControlPlane.GetTablePropertiesXml(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name);

        if (propertiesXmlOp.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.Content = new StringContent(propertiesXmlOp.Resource!, Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleGetStats(HttpContext context, HttpResponseMessage response)
    {
        // GetStatistics is only valid on the -secondary endpoint of an RA-GRS/RA-GZRS account.
        if (!TryGetStorageAccountFromSecondaryHost(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        if (!IsRaGrsAccount(storageAccount!))
        {
            const string errorXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                    "<Error><Code>FeatureNotSupported</Code>" +
                                    "<Message>The account does not support the specified HTTP verb.</Message></Error>";
            response.StatusCode = HttpStatusCode.Forbidden;
            response.Content = new StringContent(errorXml, Encoding.UTF8, "application/xml");
            return;
        }

        var statsXml = TableServiceControlPlane.GetTableServiceStatsXml();
        response.Content = new StringContent(statsXml, Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private static bool IsRaGrsAccount(Models.StorageAccountResource storageAccount)
    {
        var skuName = storageAccount.Sku?.Name;
        return string.Equals(skuName, StorageSkuName.StandardRagrs.ToString(), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(skuName, StorageSkuName.StandardRagzrs.ToString(), StringComparison.OrdinalIgnoreCase);
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

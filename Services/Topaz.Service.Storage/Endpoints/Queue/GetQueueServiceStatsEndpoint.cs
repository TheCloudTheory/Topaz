using System.Net;
using System.Text;
using Azure.ResourceManager.Storage.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class GetQueueServiceStatsEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["GET /?restype=service&comp=stats"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultQueueStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
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

        var statsXml = QueueServiceControlPlane.GetQueueServiceStatsXml();
        response.Content = new StringContent(statsXml, Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }

    private static bool IsRaGrsAccount(StorageAccountResource storageAccount)
    {
        var skuName = storageAccount.Sku?.Name;
        return string.Equals(skuName, StorageSkuName.StandardRagrs.ToString(), StringComparison.OrdinalIgnoreCase) ||
               string.Equals(skuName, StorageSkuName.StandardRagzrs.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

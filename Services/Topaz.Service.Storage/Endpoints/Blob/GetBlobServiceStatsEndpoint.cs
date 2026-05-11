using Topaz.EventPipeline;
using System.Net;
using System.Text;
using Azure.ResourceManager.Storage.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class GetBlobServiceStatsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["GET /?restype=service&comp=stats"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultBlobStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccountFromSecondaryHost(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        if (!IsRaGrsAccount(storageAccount!))
        {
            // Non-RAGRS accounts have no secondary endpoint — treat as not found.
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();
        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
            return;

        var statsXml = BlobServiceControlPlane.GetBlobServiceStatsXml();
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

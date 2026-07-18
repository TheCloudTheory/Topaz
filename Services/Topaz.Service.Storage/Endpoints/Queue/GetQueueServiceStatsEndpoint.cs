using Topaz.EventPipeline;
using System.Net;
using System.Text;
using Azure.ResourceManager.Storage.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class GetQueueServiceStatsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : QueueDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    public string ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["GET /?restype=service&comp=stats"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/read"];

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

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();
        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
            return;

        var statsXml = QueueServiceControlPlane.GetQueueServiceStatsXml(storageAccount!.Properties.LastGeoSyncTime);
        response.Content = new StringContent(statsXml, Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }


}

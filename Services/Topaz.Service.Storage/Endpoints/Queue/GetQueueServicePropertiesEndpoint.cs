using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class GetQueueServicePropertiesEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);

    public string[] Endpoints => ["GET /?restype=service&comp=properties"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultQueueStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        var result = _controlPlane.GetQueueServicePropertiesXml(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name);

        response.Content = new StringContent(result.Resource!, Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }
}

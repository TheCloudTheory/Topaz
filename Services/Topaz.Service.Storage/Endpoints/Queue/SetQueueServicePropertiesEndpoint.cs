using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class SetQueueServicePropertiesEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);

    public string[] Endpoints => ["PUT /?restype=service&comp=properties"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/write"];

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

        _controlPlane.SetQueueServiceProperties(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name, context.Request.Body);

        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
        response.StatusCode = HttpStatusCode.Accepted;
    }
}

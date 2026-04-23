using System.Net;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class ListQueuesEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = new(new QueueResourceProvider(logger), logger);

    public string[] Endpoints => ["GET /?comp=list"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/read"];

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

        try
        {
            Logger.LogDebug(nameof(ListQueuesEndpoint), nameof(GetResponse),
                "Handling listing queues for {0}.", storageAccount!.Name);

            var op = _controlPlane.ListQueues(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name);

            if (op.Result != OperationResult.Success || op.Resource == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var sw = new EncodingAwareStringWriter();
            var serializer = new XmlSerializer(typeof(QueueEnumerationResult));
            serializer.Serialize(sw, new QueueEnumerationResult(storageAccount.Name, op.Resource));

            response.Content = new StringContent(sw.ToString(), Encoding.UTF8, "application/xml");
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}

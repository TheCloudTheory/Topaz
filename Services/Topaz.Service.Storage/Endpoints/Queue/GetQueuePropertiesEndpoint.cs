using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class GetQueuePropertiesEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = new(new QueueResourceProvider(logger), logger);
    private readonly QueueServiceDataPlane _dataPlane = new(new QueueServiceControlPlane(new QueueResourceProvider(logger), logger), logger);

    public string[] Endpoints => ["HEAD /{queue-name}"];

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
            if (!TryGetQueueNameFromPath(context.Request.Path, out var queueName) || string.IsNullOrEmpty(queueName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            Logger.LogDebug(nameof(GetQueuePropertiesEndpoint), nameof(GetResponse),
                "Getting properties for queue: {0}.", queueName);

            var result = _dataPlane.GetQueueProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName);

            if (result.Result == OperationResult.Success && result.Resource != null)
            {
                response.Content = new ByteArrayContent([]);
                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("x-ms-approximate-messages-count", result.Resource.ApproximateMessageCount.ToString());
                Logger.LogDebug(nameof(GetQueuePropertiesEndpoint), nameof(GetResponse), "Queue {0} properties retrieved.", queueName);
            }
            else
            {
                response.StatusCode = HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}

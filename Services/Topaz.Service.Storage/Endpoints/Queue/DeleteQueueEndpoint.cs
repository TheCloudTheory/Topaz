using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class DeleteQueueEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["DELETE /{queue-name}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/delete"];

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

            Logger.LogDebug(nameof(DeleteQueueEndpoint), nameof(GetResponse),
                "Attempting to delete queue: {0}.", queueName);

            if (!_controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccount.Name, queueName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var result = _dataPlane.DeleteQueue(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName);

            if (result.Result == OperationResult.Success)
            {
                response.Content = new ByteArrayContent([]);
                response.StatusCode = HttpStatusCode.NoContent;
                Logger.LogDebug(nameof(DeleteQueueEndpoint), nameof(GetResponse), "Queue {0} deleted.", queueName);
            }
            else
            {
                response.Content = new ByteArrayContent([]);
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new ByteArrayContent([]);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}

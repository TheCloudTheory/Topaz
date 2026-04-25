using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

/// <summary>
/// DELETE /{queue-name}/messages/{message-id} endpoint for Azure Queue Storage.
/// Deletes a message from the queue.
/// </summary>
internal sealed class DeleteMessageEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["DELETE /{queue-name}/messages/{message-id}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/messages/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultQueueStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        try
        {
            if (!TryGetQueueNameFromPath(context.Request.Path, out var queueName) || string.IsNullOrEmpty(queueName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            // Extract message ID from path: /queuename/messages/{message-id}
            var pathParts = context.Request.Path.Value!.TrimStart('/').Split('/');
            if (pathParts.Length < 3)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            var messageId = pathParts[2];
            if (string.IsNullOrEmpty(messageId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            // Get pop receipt from query parameter
            var popReceipt = context.Request.Query["popreceipt"].FirstOrDefault();
            if (string.IsNullOrEmpty(popReceipt))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            Logger.LogDebug(nameof(DeleteMessageEndpoint), nameof(GetResponse),
                "Attempting to delete message {0} from queue {1}.", messageId, queueName);

            // Delete the message
            var result = _dataPlane.DeleteMessage(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName, messageId);

            switch (result.Result)
            {
                case OperationResult.Success:
                {
                    response.Content = new ByteArrayContent([]);
                    response.StatusCode = HttpStatusCode.NoContent; // 204 No Content per Azure spec
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

                    Logger.LogDebug(nameof(DeleteMessageEndpoint), nameof(GetResponse),
                        "Message {0} from queue {1} deleted successfully.", messageId, queueName);
                    break;
                }
                case OperationResult.NotFound:
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    break;
                default:
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
        }
    }
}

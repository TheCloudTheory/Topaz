using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class SetQueueMetadataEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["PUT /{queue-name}?comp=metadata"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/write"];

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

            Logger.LogDebug(nameof(SetQueueMetadataEndpoint), nameof(GetResponse),
                "Setting metadata for queue: {0}.", queueName);

            var result = _dataPlane.SetQueueMetadata(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName, context.Request.Headers);

            switch (result.Result)
            {
                case OperationResult.Success:
                    response.StatusCode = HttpStatusCode.NoContent;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    Logger.LogDebug(nameof(SetQueueMetadataEndpoint), nameof(GetResponse),
                        "Metadata set for queue {0}.", queueName);
                    break;
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

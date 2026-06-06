using Topaz.EventPipeline;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class CreateQueueEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : QueueDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["PUT /{queue-name}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/write"];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (RejectIfSecondaryHostForMutation(context.Request.Headers, response)) return;
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
            return;

        try
        {
            if (!TryGetQueueNameFromPath(context.Request.Path, out var queueName) || string.IsNullOrEmpty(queueName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            Logger.LogDebug(nameof(CreateQueueEndpoint), nameof(GetResponse),
                "Attempting to create queue: {0}.", queueName);

            if (_controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccount.Name, queueName))
            {
                response.StatusCode = HttpStatusCode.Conflict;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            var result = _dataPlane.CreateQueue(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName);

            if (result.Result == OperationResult.Created)
            {
                response.Content = new ByteArrayContent([]);
                response.StatusCode = HttpStatusCode.Created;
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                Logger.LogDebug(nameof(CreateQueueEndpoint), nameof(GetResponse), "Queue {0} created.", queueName);
            }
            else
            {
                response.Content = new ByteArrayContent([]);
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new ByteArrayContent([]);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
        }
    }
}

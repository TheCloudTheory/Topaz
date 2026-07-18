using Topaz.EventPipeline;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class GetQueuePropertiesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : QueueDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["GET /{queue-name}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/read"];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out _))
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

            Logger.LogDebug(nameof(GetQueuePropertiesEndpoint), nameof(GetResponse),
                "Getting properties for queue: {0}.", queueName);

            var result = _dataPlane.GetQueueProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName);

            if (result.Result == OperationResult.Success && result.Resource != null)
            {
                response.Content = new ByteArrayContent([]);
                response.StatusCode = HttpStatusCode.OK;
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                response.Headers.Add("x-ms-approximate-messages-count", result.Resource.ApproximateMessageCount.ToString());

                if (result.Resource.Metadata != null)
                {
                    foreach (var (key, value) in result.Resource.Metadata)
                        response.Headers.Add($"x-ms-meta-{key}", value);
                }

                Logger.LogDebug(nameof(GetQueuePropertiesEndpoint), nameof(GetResponse), "Queue {0} properties retrieved.", queueName);
            }
            else
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
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

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class GetQueueAclEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["GET /{queue-name}?comp=acl"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/read"];

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

            Logger.LogDebug(nameof(GetQueueAclEndpoint), nameof(GetResponse),
                "Getting ACL for queue: {0}.", queueName);

            var result = _dataPlane.GetQueueAcl(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName);

            if (result.Result != OperationResult.Success)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            response.StatusCode = HttpStatusCode.OK;
            response.Headers.ETag = new EntityTagHeaderValue($"\"{now.Ticks}\"");
            response.Content = new StringContent(result.Resource!, Encoding.UTF8);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            response.Content.Headers.TryAddWithoutValidation("Last-Modified", now.ToString("R"));
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

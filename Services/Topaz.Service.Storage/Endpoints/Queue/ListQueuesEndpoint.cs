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

            // Build XML manually to avoid XmlSerializer issues
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append($"<EnumerationResults ServiceEndpoint=\"https://{storageAccount.Name}.queue.core.windows.net\">");
            sb.Append("<Prefix></Prefix>");
            sb.Append("<Marker></Marker>");
            sb.Append("<MaxResults>0</MaxResults>");
            sb.Append("<Queues>");
            
            foreach (var queue in op.Resource)
            {
                sb.Append("<Queue>");
                sb.Append($"<Name>{System.Net.WebUtility.HtmlEncode(queue.Name)}</Name>");
                sb.Append("</Queue>");
            }
            
            sb.Append("</Queues>");
            sb.Append("</EnumerationResults>");

            response.Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/xml");
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Logger.LogError($"{ex.GetType().Name}: {ex.Message}. InnerException: {ex.InnerException?.Message}");
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

internal sealed class PeekMessagesEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["GET /{queue-name}/messages?peekonly=true"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/messages/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultQueueStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            Logger.LogError(nameof(PeekMessagesEndpoint), nameof(GetResponse), "TryGetStorageAccount failed");
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
                Logger.LogError(nameof(PeekMessagesEndpoint), nameof(GetResponse),
                    "TryGetQueueNameFromPath failed for path: {0}", context.Request.Path);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            var numOfMessagesParam = context.Request.Query["numofmessages"].FirstOrDefault();

            int numMessages = 1;
            if (!string.IsNullOrEmpty(numOfMessagesParam))
            {
                if (!int.TryParse(numOfMessagesParam, out numMessages) || numMessages < 1 || numMessages > 32)
                {
                    Logger.LogDebug(nameof(PeekMessagesEndpoint), nameof(GetResponse),
                        "Invalid numofmessages parameter: {0}", numOfMessagesParam);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }
            }

            var result = _dataPlane.PeekMessages(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName, numMessages);

            switch (result.Result)
            {
                case OperationResult.Success when result.Resource != null:
                {
                    var xmlResponse = GeneratePeekMessagesResponse(result.Resource);
                    response.Content = new StringContent(xmlResponse, Encoding.UTF8, "application/xml");
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    break;
                }
                case OperationResult.NotFound:
                    Logger.LogError(nameof(PeekMessagesEndpoint), nameof(GetResponse), "Queue not found: {0}", queueName);
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    break;
                default:
                    Logger.LogError(nameof(PeekMessagesEndpoint), nameof(GetResponse), "Internal server error");
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

    private static string GeneratePeekMessagesResponse(List<QueueMessage> messages)
    {
        var messageItems = messages.Select(message => new QueueMessageResponseItem
        {
            MessageId = message.Id,
            InsertionTime = message.EnqueuedTime?.ToString("R"),
            ExpirationTime = message.ExpiryTime?.ToString("R"),
            DequeueCount = message.DequeueCount,
            MessageText = message.Content
            // PopReceipt and TimeNextVisible intentionally omitted for peek
        }).ToList();

        var peekResponse = new QueueMessagesResponse { Messages = messageItems };

        using var memoryStream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(QueueMessagesResponse));
        serializer.Serialize(memoryStream, peekResponse);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}

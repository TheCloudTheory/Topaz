using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

/// <summary>
/// GET /{queue-name}/messages endpoint for Azure Queue Storage.
/// Retrieves one or more messages from the front of a queue.
/// Messages are hidden during their visibility timeout and dequeue count is incremented.
/// </summary>
internal sealed class GetMessagesEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["GET /{queue-name}/messages"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/messages/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultQueueStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            Logger.LogError(nameof(GetMessagesEndpoint), nameof(GetResponse), "TryGetStorageAccount failed");
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
                Logger.LogError(nameof(GetMessagesEndpoint), nameof(GetResponse), "TryGetQueueNameFromPath failed for path: {0}", context.Request.Path);
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            Logger.LogDebug(nameof(GetMessagesEndpoint), nameof(GetResponse),
                "Attempting to retrieve messages from queue {0}.", queueName);

            // Parse query parameters
            var numOfMessagesParam = context.Request.Query["numofmessages"].FirstOrDefault();
            var visibilityTimeoutParam = context.Request.Query["visibilitytimeout"].FirstOrDefault();

            // Parse and validate numofmessages (1-32, default 1)
            int numMessages = 1;
            if (!string.IsNullOrEmpty(numOfMessagesParam))
            {
                if (!int.TryParse(numOfMessagesParam, out numMessages) || numMessages < 1 || numMessages > 32)
                {
                    Logger.LogDebug(nameof(GetMessagesEndpoint), nameof(GetResponse),
                        "Invalid numofmessages parameter: {0}", numOfMessagesParam);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }
            }

            // Parse and validate visibilitytimeout (0-604800, default 30)
            int visibilityTimeout = 30;
            if (!string.IsNullOrEmpty(visibilityTimeoutParam))
            {
                if (!int.TryParse(visibilityTimeoutParam, out visibilityTimeout))
                {
                    Logger.LogDebug(nameof(GetMessagesEndpoint), nameof(GetResponse),
                        "Invalid visibilitytimeout parameter: {0}", visibilityTimeoutParam);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }

                // Validate visibility timeout range
                if (!QueueMessageValidator.ValidateVisibilityTimeout(visibilityTimeout, out var timeoutError))
                {
                    Logger.LogDebug(nameof(GetMessagesEndpoint), nameof(GetResponse),
                        "Invalid visibility timeout: {0}", timeoutError);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }
            }

            // Retrieve messages from the queue
            var result = _dataPlane.GetMessages(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName, numMessages, visibilityTimeout);

            Logger.LogDebug(nameof(GetMessagesEndpoint), nameof(GetResponse), 
                "GetMessages result: {0} (should be Success or NotFound), messages count: {1}", 
                result.Result, result.Resource?.Count ?? -1);
            
            switch (result.Result)
            {
                case OperationResult.Success when result.Resource != null:
                {
                    var xmlResponse = GenerateMessagesResponse(result.Resource);
                    response.Content = new StringContent(xmlResponse, Encoding.UTF8, "application/xml");
                    response.StatusCode = HttpStatusCode.OK;
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

                    Logger.LogDebug(nameof(GetMessagesEndpoint), nameof(GetResponse),
                        "Retrieved {0} messages from queue {1}.", result.Resource.Count, queueName);
                    break;
                }
                case OperationResult.NotFound:
                    Logger.LogError(nameof(GetMessagesEndpoint), nameof(GetResponse), "Queue not found: {0}", queueName);
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    break;
                default:
                    Logger.LogError(nameof(GetMessagesEndpoint), nameof(GetResponse), "Internal server error");
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

    /// <summary>
    /// Generate XML response for get messages operation per Azure Queue Storage API.
    /// </summary>
    private static string GenerateMessagesResponse(List<QueueMessage> messages)
    {
        var messageItems = messages.Select(message => new QueueMessageResponseItem
        {
            MessageId = message.Id,
            InsertionTime = message.EnqueuedTime?.ToString("R"), // RFC 1123 format
            ExpirationTime = message.ExpiryTime?.ToString("R"),   // RFC 1123 format
            PopReceipt = message.PopReceipt,
            TimeNextVisible = message.NextVisibleTime?.ToString("R"), // RFC 1123 format
            DequeueCount = message.DequeueCount,
            MessageText = message.Content
        }).ToList();

        var response = new QueueMessagesResponse
        {
            Messages = messageItems
        };

        using var memoryStream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(QueueMessagesResponse));
        serializer.Serialize(memoryStream, response);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

/// <summary>
/// POST /{queue-name}/messages endpoint for Azure Queue Storage.
/// Enqueues a new message to the back of the queue.
/// </summary>
internal sealed class SendMessageEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["POST /{queue-name}/messages"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/messages/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultQueueStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        Logger.LogInformation($"[SendMessageEndpoint] GetResponse called with path: {context.Request.Path} method: {context.Request.Method}");
        try
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

            if (!TryGetQueueNameFromPath(context.Request.Path, out var queueName) || string.IsNullOrEmpty(queueName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                "Attempting to enqueue message to queue {0}.", queueName);

            // Parse query parameters
            var visibilityTimeoutParam = context.Request.Query["visibilitytimeout"].FirstOrDefault();
            var messageTtlParam = context.Request.Query["messagettl"].FirstOrDefault();

            // Parse and validate visibilitytimeout (0-604800, default 0)
            int visibilityTimeout = 0;
            if (!string.IsNullOrEmpty(visibilityTimeoutParam))
            {
                if (!int.TryParse(visibilityTimeoutParam, out visibilityTimeout))
                {
                    Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                        "Invalid visibilitytimeout parameter: {0}", visibilityTimeoutParam);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }

                // Validate visibility timeout range
                if (!QueueMessageValidator.ValidateVisibilityTimeout(visibilityTimeout, out var timeoutError))
                {
                    Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                        "Invalid visibility timeout: {0}", timeoutError);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }
            }

            // Parse and validate messagettl (default 604800 = 7 days)
            int messageTtl = 604800;
            if (!string.IsNullOrEmpty(messageTtlParam))
            {
                if (!int.TryParse(messageTtlParam, out messageTtl))
                {
                    Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                        "Invalid messagettl parameter: {0}", messageTtlParam);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }

                // Validate TTL is positive
                if (messageTtl <= 0 && messageTtl != -1)
                {
                    Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                        "Invalid messagettl: must be positive or -1 for never expires");
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }

                // If messagettl is -1, set to maximum (never expires)
                if (messageTtl == -1)
                {
                    messageTtl = int.MaxValue;
                }
            }

            // Read request body (XML format)
            string messageContent = string.Empty;
            if (context.Request.ContentLength is > 0)
            {
                using var reader = new StreamReader(context.Request.Body);
                var xmlContent = reader.ReadToEnd();

                // Parse XML to extract MessageText
                if (!string.IsNullOrEmpty(xmlContent))
                {
                    try
                    {
                        var doc = XDocument.Parse(xmlContent);
                        var messageTextElement = doc.Root?.Element("MessageText");
                        
                        if (messageTextElement != null)
                        {
                            messageContent = messageTextElement.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                            "Failed to parse XML message body: {0}", ex.Message);
                        response.StatusCode = HttpStatusCode.BadRequest;
                        response.Content = new ByteArrayContent([]);
                        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                        return;
                    }
                }
            }

            // Validate message size
            if (!QueueMessageValidator.ValidateMessageSize(messageContent, out var sizeError))
            {
                Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                    "Message size validation failed: {0}", sizeError);
                response.StatusCode = QueueMessageValidator.GetPayloadTooLargeStatusCode();
                response.Content = new StringContent(
                    $"<Error><Code>{QueueMessageValidator.GetPayloadTooLargeErrorCode()}</Code><Message>{sizeError}</Message></Error>",
                    Encoding.UTF8,
                    "application/xml");
                return;
            }

            // Send the message (enqueue)
            var result = _dataPlane.SendMessage(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName, messageContent, visibilityTimeout, messageTtl);

            switch (result.Result)
            {
                case OperationResult.Success when result.Resource != null:
                {
                    var xmlResponse = GenerateMessageResponse(result.Resource);
                    response.Content = new StringContent(xmlResponse, Encoding.UTF8, "application/xml");
                    response.StatusCode = HttpStatusCode.Created; // 201 Created per Azure spec
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

                    Logger.LogDebug(nameof(SendMessageEndpoint), nameof(GetResponse),
                        "Message enqueued to queue {0} with ID {1}.", queueName, result.Resource.Id);
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

    /// <summary>
    /// Generate XML response for send message operation per Azure Queue Storage API.
    /// Returns the message info wrapped in QueueMessagesList for consistency with Get Messages.
    /// </summary>
    private static string GenerateMessageResponse(QueueMessage message)
    {
        var messageItem = new QueueMessageResponseItem
        {
            MessageId = message.Id,
            InsertionTime = message.EnqueuedTime?.ToString("R"), // RFC 1123 format
            ExpirationTime = message.ExpiryTime?.ToString("R"),   // RFC 1123 format
            PopReceipt = message.PopReceipt,
            TimeNextVisible = message.NextVisibleTime?.ToString("R") // RFC 1123 format
        };

        var response = new QueueMessagesResponse
        {
            Messages = new List<QueueMessageResponseItem> { messageItem }
        };

        using var memoryStream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(QueueMessagesResponse));
        serializer.Serialize(memoryStream, response);
        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}

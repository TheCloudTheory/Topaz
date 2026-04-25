using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Queue;

/// <summary>
/// PUT /{queue-name}/messages/{message-id} endpoint for Azure Queue Storage.
/// Updates a message's visibility timeout and/or content.
/// </summary>
internal sealed class PutMessageEndpoint(ITopazLogger logger)
    : QueueDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly QueueServiceControlPlane _controlPlane = QueueServiceControlPlane.New(logger);
    private readonly QueueServiceDataPlane _dataPlane = QueueServiceDataPlane.New(logger);

    public string[] Endpoints => ["PUT /{queue-name}/messages/{message-id}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/queueServices/queues/messages/write"];

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

            Logger.LogDebug(nameof(PutMessageEndpoint), nameof(GetResponse),
                "Attempting to update message {0} in queue {1}.", messageId, queueName);

            // Check if queue exists
            if (!_controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccount.Name, queueName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            // Get visibility timeout from query parameter (default 30 seconds)
            var visibilityTimeoutParam = context.Request.Query["visibilitytimeout"].FirstOrDefault();
            int visibilityTimeout = 30;

            if (!string.IsNullOrEmpty(visibilityTimeoutParam))
            {
                if (!int.TryParse(visibilityTimeoutParam, out visibilityTimeout))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
                }

                // Validate visibility timeout
                if (!QueueMessageValidator.ValidateVisibilityTimeout(visibilityTimeout, out var timeoutError))
                {
                    Logger.LogDebug(nameof(PutMessageEndpoint), nameof(GetResponse),
                        "Invalid visibility timeout: {0}", timeoutError);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    return;
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
                        Logger.LogDebug(nameof(PutMessageEndpoint), nameof(GetResponse),
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
                Logger.LogDebug(nameof(PutMessageEndpoint), nameof(GetResponse),
                    "Message size validation failed: {0}", sizeError);
                response.StatusCode = QueueMessageValidator.GetPayloadTooLargeStatusCode();
                response.Content = new StringContent(
                    $"<Error><Code>{QueueMessageValidator.GetPayloadTooLargeErrorCode()}</Code><Message>{sizeError}</Message></Error>",
                    Encoding.UTF8,
                    "application/xml");
                return;
            }

            // Put the message
            var result = _dataPlane.PutMessage(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name, queueName, messageId, messageContent, visibilityTimeout);

            switch (result.Result)
            {
                case OperationResult.Success when result.Resource != null:
                {
                    response.StatusCode = HttpStatusCode.NoContent;
                    response.Content = new ByteArrayContent([]);
                    response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                    response.Headers.Add("x-ms-popreceipt", result.Resource.PopReceipt ?? string.Empty);
                    if (result.Resource.NextVisibleTime.HasValue)
                    {
                        response.Headers.Add("x-ms-time-next-visible",
                            result.Resource.NextVisibleTime.Value.ToString("R"));
                    }

                    Logger.LogDebug(nameof(PutMessageEndpoint), nameof(GetResponse),
                        "Message {0} in queue {1} updated successfully.", messageId, queueName);
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
            Logger.LogError(nameof(PutMessageEndpoint), nameof(GetResponse), "Exception in PutMessageEndpoint: {0}", ex.Message);
            Logger.LogError(nameof(PutMessageEndpoint), nameof(GetResponse), "Stack trace: {0}", ex.StackTrace);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
        }
    }

}

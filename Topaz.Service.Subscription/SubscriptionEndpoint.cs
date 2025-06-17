using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Shared;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Subscription.Models.Requests;
using Topaz.Service.Subscription.Models.Responses;

namespace Topaz.Service.Subscription;

public sealed class SubscriptionEndpoint(ResourceProvider provider, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = new(provider);
    public string[] Endpoints => [
        "GET /subscriptions/{subscriptionId}",
        "POST /subscriptions/{subscriptionId}",
        "GET /subscriptions"
    ];
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");

        var response = new HttpResponseMessage();

        try
        {
            switch (method)
            {
                case "GET":
                    var subscriptionId = path.ExtractValueFromPath(2);
                    if (string.IsNullOrEmpty(subscriptionId))
                    {
                        HandleListSubscriptionsRequest(response);
                    }
                    else
                    {
                        HandleGetSubscriptionRequest(path, response);
                    }
                    
                    break;
                case "POST":
                    HandleCreateSubscriptionRequest(path, input, response);
                    break;
                default:
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;

            return response;
        }
        
        return response;
    }

    private void HandleListSubscriptionsRequest(HttpResponseMessage response)
    {
        var operation = _controlPlane.List();
        if (operation.result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var subscriptions = new ListSubscriptionsResponse(operation.resource);
        
        response.Content = new StringContent(subscriptions.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateSubscriptionRequest(string path, Stream input, HttpResponseMessage response)
    {
        var subscriptionId = path.ExtractValueFromPath(2);
        if (subscriptionId == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        var subscription = _controlPlane.Get(subscriptionId);
        if (subscription is not null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent($"Subscription with ID {subscriptionId} already exists.");

            return;
        }
        
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateSubscriptionRequest>(content, GlobalSettings.JsonOptions);

        if (request?.SubscriptionId == null || string.IsNullOrWhiteSpace(request.SubscriptionName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        _controlPlane.Create(subscriptionId, request.SubscriptionName);
        response.StatusCode = HttpStatusCode.Created;
    }

    private void HandleGetSubscriptionRequest(string path, HttpResponseMessage response)
    {
        var subscriptionId = path.ExtractValueFromPath(2);
        if (subscriptionId == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        var subscription = _controlPlane.Get(subscriptionId);

        response.Content = JsonContent.Create(subscription, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
        response.StatusCode = HttpStatusCode.OK;
    }
}

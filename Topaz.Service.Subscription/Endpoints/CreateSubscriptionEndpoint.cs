using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Subscription.Endpoints;

public class CreateSubscriptionEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        if (subscriptionId == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent($"Subscription ID can't be null.");
            return;
        }

        var subscriptionIdentifier = SubscriptionIdentifier.From(subscriptionId);
        var subscription = _controlPlane.Get(subscriptionIdentifier);
        if (subscription.Result is not OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent($"Subscription with ID {subscriptionId} already exists.");

            return;
        }

        using var reader = new StreamReader(context.Request.Body);

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateSubscriptionRequest>(content, GlobalSettings.JsonOptions);

        if (request?.SubscriptionId == null || string.IsNullOrWhiteSpace(request.SubscriptionName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        _controlPlane.Create(subscriptionIdentifier, request.SubscriptionName);
        response.StatusCode = HttpStatusCode.Created;
    }
}
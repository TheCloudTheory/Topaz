using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ApiManagement.Endpoints;

internal sealed class CheckNameAvailabilityEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApiManagementServiceControlPlane _controlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.AppConfiguration";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.ApiManagement/checkNameAvailability"
    ];

    public string[] Permissions => ["Microsoft.ApiManagement/service/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<CheckNameAvailabilityRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var validationResult = request.Validate<CheckNameAvailabilityRequest>();
        if (!validationResult.IsValid)
        {
            response.CreateErrorResponse(validationResult.Error!, HttpStatusCode.BadRequest);
            return;
        }

        var result = _controlPlane.CheckNameAvailability(sub, request);
        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.CreateJsonContentResponse(result.Resource);
    }
}
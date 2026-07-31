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

internal sealed class UpdateApiManagementServiceEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApiManagementServiceControlPlane _controlPlane =
        ApiManagementServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ApiManagement";

    public string[] Endpoints =>
    [
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}"
    ];

    public string[] Permissions => ["Microsoft.ApiManagement/service/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(name))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<CreateOrUpdateApiManagementServiceRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.Update(sub, rg, name, request);
        if (result.Result != OperationResult.Updated || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.CreateJsonContentResponse(result.Resource);
    }
}
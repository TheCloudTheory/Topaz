using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ApiManagement.Endpoints.DataPlane.Backend;

internal sealed class UpdateBackendEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApiManagementBackendControlPlane _controlPlane =
        ApiManagementBackendControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.ApiManagement";

    public string[] Endpoints =>
    [
        "PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/backends/{backendId}"
    ];

    public string[] Permissions => ["Microsoft.ApiManagement/service/backends/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);
        var backendId = context.Request.Path.Value.ExtractValueFromPath(10);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(backendId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateBackendRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var ifMatch = context.Request.Headers.TryGetValue("If-Match", out var value)
            ? value.ToString()
            : null;

        var result = _controlPlane.Update(sub, rg, name, backendId, request, ifMatch);
        switch (result.Result)
        {
            case OperationResult.NotFound:
                response.CreateNotFoundResponse(result);
                return;
            case OperationResult.BadRequest:
                response.CreateErrorResponse(result, HttpStatusCode.BadRequest);
                return;
            case OperationResult.Conflict:
                response.CreateErrorResponse(result, HttpStatusCode.Conflict);
                return;
        }

        if (result.Result != OperationResult.Updated || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.CreateJsonContentResponse(result.Resource);
    }
}
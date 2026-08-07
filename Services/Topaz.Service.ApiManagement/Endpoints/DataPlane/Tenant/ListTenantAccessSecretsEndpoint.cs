using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ApiManagement.Endpoints.DataPlane.Tenant;

internal sealed class ListTenantAccessSecretsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly TenantControlPlane _controlPlane =
        TenantControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.ApiManagement";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ApiManagement/service/{serviceName}/tenant/access/listSecrets",
    ];

    public string[] Permissions => ["Microsoft.ApiManagement/service/tenant/listSecrets/action"];

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

        var result = _controlPlane.ListTenantAccessSecrets(sub, rg, name);
        switch (result.Result)
        {
            case OperationResult.NotFound:
                response.CreateNotFoundResponse(result);
                return;
            case OperationResult.BadRequest:
                response.CreateErrorResponse(result, HttpStatusCode.BadRequest);
                return;
        }

        if (result.Result is not OperationResult.Success || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }

        response.CreateJsonContentResponse(result.Resource);
    }
}
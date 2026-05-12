using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleAssignments;

/// <summary>
/// Handles resource-scoped role assignment creation/update for single-level resources.
/// PUT /subscriptions/{subId}/resourceGroups/{rg}/providers/{ns}/{type}/{resourceName}/providers/Microsoft.Authorization/roleAssignments/{name}
/// </summary>
internal sealed class CreateUpdateRoleAssignmentAtResourceEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Authorization";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{providerNamespace}/{resourceType}/{resourceName}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleAssignments/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);

        var path = context.Request.Path.Value!;
        // /subscriptions/{0}/resourceGroups/{1}/providers/{2}/{3}/{4}/providers/Microsoft.Authorization/roleAssignments/{5}
        // Indices after split by '/':
        //  [0]=""  [1]="subscriptions"  [2]=subId  [3]="resourceGroups"  [4]=rg
        //  [5]="providers"  [6]=providerNs  [7]=resourceType  [8]=resourceName
        //  [9]="providers"  [10]="Microsoft.Authorization"  [11]="roleAssignments"  [12]=assignmentName
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var roleAssignmentName = RoleAssignmentName.From(path.ExtractValueFromPath(12));

        // Build the scope from the resource path segments (everything up to and including resourceName)
        var segments = path.Split('/');
        var scope = string.Join("/", segments.Take(9)); // yields "/subscriptions/{sub}/resourceGroups/{rg}/providers/{ns}/{type}/{name}"

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateRoleAssignmentRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateRoleAssignment(subscriptionIdentifier, roleAssignmentName, request, scope);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}

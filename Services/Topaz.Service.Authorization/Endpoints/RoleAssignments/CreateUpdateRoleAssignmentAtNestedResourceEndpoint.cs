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
/// Handles resource-scoped role assignment creation/update for two-level nested resources
/// (e.g. Cosmos DB sqlDatabases/{db} or sqlDatabases/{db}/containers/{coll}).
///
/// Covers:
///   PUT .../providers/{ns}/{type}/{name}/{subType}/{subName}/providers/Microsoft.Authorization/roleAssignments/{id}
///   PUT .../providers/{ns}/{type}/{name}/{subType}/{subName}/{subSubType}/{subSubName}/providers/Microsoft.Authorization/roleAssignments/{id}
/// </summary>
internal sealed class CreateUpdateRoleAssignmentAtNestedResourceEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Authorization";

    public string[] Endpoints =>
    [
        // Two-level nested resource (e.g. databaseAccounts/{account}/sqlDatabases/{db})
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{providerNamespace}/{resourceType}/{resourceName}/{subResourceType}/{subResourceName}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}",
        // Three-level nested resource (e.g. databaseAccounts/{account}/sqlDatabases/{db}/containers/{coll})
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{providerNamespace}/{resourceType}/{resourceName}/{subResourceType}/{subResourceName}/{subSubResourceType}/{subSubResourceName}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}"
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleAssignments/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);

        var path = context.Request.Path.Value!;
        var segments = path.Split('/');
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));

        // Find "providers/Microsoft.Authorization/roleAssignments/{name}" suffix.
        // The scope is everything up to (but not including) the second "/providers/" insertion.
        var authIdx = IndexOfAuthorizationProviders(segments);
        if (authIdx < 0)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var scope = string.Join("/", segments.Take(authIdx));
        var roleAssignmentName = RoleAssignmentName.From(segments[^1]);

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
                "Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    /// <summary>
    /// Returns the index of the segment just before ".../providers/Microsoft.Authorization/roleAssignments/...".
    /// </summary>
    private static int IndexOfAuthorizationProviders(string[] segments)
    {
        for (var i = 0; i < segments.Length - 2; i++)
        {
            if (string.Equals(segments[i], "providers", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[i + 1], "Microsoft.Authorization", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(segments[i + 2], "roleAssignments", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}

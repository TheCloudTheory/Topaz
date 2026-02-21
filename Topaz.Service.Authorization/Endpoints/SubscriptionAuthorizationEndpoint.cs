using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Authorization.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints;

public sealed class SubscriptionAuthorizationEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(logger);
    
    public string[] Endpoints => [
        "PUT /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}",
        "PUT /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}",
        "PUT /{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions",
        "GET /{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions",
        "DELETE /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleDefinitionId}",
        "DELETE /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleAssignments/{roleAssignmentName}"
    ];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();
        
        try
        {
            var segmentsCount = path.Split('/').Length;
            var subscriptionIdentifier = path.StartsWith("/subscriptions")
                ? SubscriptionIdentifier.From(path.ExtractValueFromPath(2))
                : SubscriptionIdentifier.From(path.ExtractValueFromPath(1));
            var isRoleDefinition = path.Contains("roleDefinitions");
            var roleDefinitionId = segmentsCount > 6 ? RoleDefinitionIdentifier.From(path.ExtractValueFromPath(6)) : null;
            var roleAssignmentName = ExtractRoleAssignmentNameFromPath(path, isRoleDefinition, segmentsCount);
            
            switch (method)
            {
                case "PUT":
                {
                    if (isRoleDefinition)
                    {
                        HandleCreateUpdateRoleDefinition(response, subscriptionIdentifier, roleDefinitionId!, input);
                        break;
                    }

                    HandleCreateUpdateRoleAssignment(response, subscriptionIdentifier, roleAssignmentName!, input);
                    break;
                }
                case "GET":
                {
                    if (isRoleDefinition)
                    {
                        if (roleDefinitionId == null)
                        {
                            HandleListRoleDefinition(response, subscriptionIdentifier, query);
                            break;
                        }
                        
                        HandleGetRoleDefinition(response, subscriptionIdentifier, roleDefinitionId);
                        break;
                    }

                    if (roleAssignmentName == null)
                    {
                        HandleListRoleAssignments(response, subscriptionIdentifier, query);
                        break;
                    }
                    
                    HandleGetRoleAssignment(response, subscriptionIdentifier, roleAssignmentName!);
                    break;
                }
                case "DELETE":
                {
                    if (isRoleDefinition)
                    {
                        HandleDeleteRoleDefinition(response, subscriptionIdentifier, roleDefinitionId!);
                        break;
                    }
                    
                    HandleDeleteRoleAssignment(response, subscriptionIdentifier, roleAssignmentName!);
                    break;
                }
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
        }
        
        return response;
    }

    private static RoleAssignmentName? ExtractRoleAssignmentNameFromPath(string path, bool isRoleDefinition, int segmentsCount)
    {
        return !isRoleDefinition && segmentsCount > 6
            ?
            RoleAssignmentName.From(path.ExtractValueFromPath(6))
            :
            !isRoleDefinition && segmentsCount > 5 && !path.StartsWith("/subscriptions/")
                ? RoleAssignmentName.From(path.ExtractValueFromPath(5))
                : null;
    }

    private void HandleListRoleAssignments(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, QueryString query)
    {
        logger.LogDebug(nameof(SubscriptionAuthorizationEndpoint), nameof(HandleListRoleAssignments),
            "Executing {0}: Attempting to list role assignments for subscription ID `{1}`.",
            nameof(HandleListRoleDefinition), subscriptionIdentifier);
        
        string? roleName = null;
        if (query.TryGetValueForKey("$filter", out var filter))
        {
            // A filter is basically an expression looking like this: $filter=roleName eq 'Contributor'
            roleName = ExtractRoleNamerFromFilter(filter);
        }
        
        var assignments = _controlPlane.ListRoleAssignmentsBySubscription(subscriptionIdentifier, roleName);
        if (assignments.Result != OperationResult.Success || assignments.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionRoleAssignmentsResponse
        {
            Value = assignments.Resource.Select(ListSubscriptionRoleAssignmentsResponse.RoleAssignment.From).ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleListRoleDefinition(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        QueryString query)
    {
        logger.LogDebug(nameof(SubscriptionAuthorizationEndpoint), nameof(HandleListRoleDefinition),
            "Executing {0}: Attempting to list role definitions for subscription ID `{1}` and query `{2}`.",
            nameof(HandleListRoleDefinition), subscriptionIdentifier, query.Value);

        string? roleName = null;
        if (query.TryGetValueForKey("$filter", out var filter))
        {
            // A filter is basically an expression looking like this: $filter=roleName eq 'Contributor'
            roleName = ExtractRoleNamerFromFilter(filter);
        }

        var definitions = _controlPlane.ListRoleDefinitionsBySubscription(subscriptionIdentifier, roleName);
        if (definitions.Result != OperationResult.Success || definitions.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionRoleDefinitionsResponse
        {
            Value = definitions.Resource.Select(ListSubscriptionRoleDefinitionsResponse.RoleDefinition.From).ToArray()
        };
        
        response.Content = new StringContent(result.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private static string? ExtractRoleNamerFromFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter)) return null;
        var segments = filter.Split(' ');
        
        return segments.Length > 1 ? segments[2].Replace("'", string.Empty) : null;
    }

    private void HandleDeleteRoleAssignment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, RoleAssignmentName roleAssignmentName)
    {
        logger.LogDebug(nameof(SubscriptionAuthorizationEndpoint), nameof(HandleDeleteRoleAssignment), "Executing {0}.", nameof(HandleDeleteRoleAssignment));
        
        var existingDefinition = _controlPlane.Get(subscriptionIdentifier, roleAssignmentName);
        switch (existingDefinition.Result)
        {
            case OperationResult.NotFound:
                // For some reason, the API for role assignments is supposed to return HTTP 204
                // when a role is already deleted or non-existing
                response.StatusCode = HttpStatusCode.NoContent;
                return;
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            default:
                var operation = _controlPlane.Delete(subscriptionIdentifier, roleAssignmentName);
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(operation.Resource!.ToString());
                break;
        }
    }

    private void HandleGetRoleAssignment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, RoleAssignmentName roleAssignmentName)
    {
        logger.LogDebug(nameof(SubscriptionAuthorizationEndpoint), nameof(HandleGetRoleAssignment), "Executing {0}.", nameof(HandleGetRoleAssignment));
        
        var operation = _controlPlane.Get(subscriptionIdentifier, roleAssignmentName);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleCreateUpdateRoleAssignment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, RoleAssignmentName roleAssignmentName, Stream input)
    {
        logger.LogDebug(nameof(SubscriptionAuthorizationEndpoint), nameof(HandleCreateUpdateRoleAssignment), "Executing {0}.", nameof(HandleCreateUpdateRoleAssignment));
        
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateRoleAssignmentRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateRoleAssignment(subscriptionIdentifier, roleAssignmentName, request);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleDeleteRoleDefinition(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, RoleDefinitionIdentifier roleDefinitionId)
    {
        logger.LogDebug(nameof(SubscriptionAuthorizationEndpoint), nameof(HandleDeleteRoleDefinition), "Executing {0}.", nameof(HandleDeleteRoleDefinition));
        
        var existingDefinition = _controlPlane.Get(subscriptionIdentifier, roleDefinitionId);
        switch (existingDefinition.Result)
        {
            case OperationResult.NotFound:
                // For some reason, the API for role definitions is supposed to return HTTP 204
                // when a role is already deleted or non-existing
                response.StatusCode = HttpStatusCode.NoContent;
                return;
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            default:
                var operation = _controlPlane.Delete(subscriptionIdentifier, roleDefinitionId);
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(operation.Resource!.ToString());
                break;
        }
    }

    private void HandleGetRoleDefinition(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, RoleDefinitionIdentifier roleDefinitionIdentifier)
    {
        logger.LogDebug(nameof(SubscriptionAuthorizationEndpoint), nameof(HandleGetRoleDefinition),
            "Looking for role {0} in subscription {1}.", roleDefinitionIdentifier, subscriptionIdentifier);
        
        var operation = _controlPlane.Get(subscriptionIdentifier, roleDefinitionIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleCreateUpdateRoleDefinition(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, RoleDefinitionIdentifier roleDefinitionId, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateRoleDefinitionRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateRoleDefinition(subscriptionIdentifier, roleDefinitionId, request);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(operation.Resource.ToString());
    }
}
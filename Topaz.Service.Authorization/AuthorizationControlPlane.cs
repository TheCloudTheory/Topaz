using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Models;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

internal sealed class AuthorizationControlPlane(
    SubscriptionControlPlane subscriptionControlPlane,
    ResourceAuthorizationResourceProvider resourceAuthorizationProvider,
    ResourceGroupAuthorizationResourceProvider resourceGroupAuthorizationProvider,
    SubscriptionAuthorizationResourceProvider subscriptionAuthorizationProvider,
    ITopazLogger logger
) : IControlPlane
{
    private const string RoleDefinitionNotFoundMessageTemplate =
        "Role definition '{0}' could not be found";
    private const string RoleDefinitionNotFoundMessageCode = "RoleDefinitionNotFound";
    private const string RoleAssignmentNotFoundMessageTemplate =
        "Role assignment '{0}' could not be found";
    private const string RoleAssignmentNotFoundMessageCode = "RoleAssignmentNotFound";

    public static AuthorizationControlPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(
        new SubscriptionControlPlane(eventPipeline, new SubscriptionResourceProvider(logger)),
        new ResourceAuthorizationResourceProvider(logger),
        new ResourceGroupAuthorizationResourceProvider(logger),
        new SubscriptionAuthorizationResourceProvider(logger),
        logger
    );
    
    public OperationResult Deploy(GenericResource resource)
    {
        throw new NotImplementedException();
    }   
    
    public ControlPlaneOperationResult<RoleDefinitionResource?> CreateOrUpdateRoleDefinition(SubscriptionIdentifier subscriptionIdentifier,
        RoleDefinitionIdentifier roleDefinitionIdentifier, CreateOrUpdateRoleDefinitionRequest request)
    {
        var subscriptionOperation = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<RoleDefinitionResource?>(OperationResult.Failed, null,
                subscriptionOperation.Reason,
                subscriptionOperation.Code);
        }

        RoleDefinitionResource resource;
        var roleDefinitionOperation = Get(subscriptionIdentifier, roleDefinitionIdentifier);
        var createOperation = roleDefinitionOperation.Result == OperationResult.NotFound;
        if (createOperation)
        {
            var properties = RoleDefinitionResourceProperties.FromRequest(request);
            properties.CreatedOn = DateTimeOffset.UtcNow;
            properties.UpdatedOn = DateTimeOffset.UtcNow;
            
            resource = new RoleDefinitionResource(subscriptionIdentifier, roleDefinitionIdentifier.Value, properties);
        }
        else
        {
            RoleDefinitionResourceProperties.UpdateFromRequest(roleDefinitionOperation.Resource!, request);
            resource = roleDefinitionOperation.Resource!;
        }
        
        subscriptionAuthorizationProvider.CreateOrUpdate(subscriptionIdentifier, null, roleDefinitionIdentifier.Value, resource, createOperation);
        
        return new ControlPlaneOperationResult<RoleDefinitionResource?>(
            createOperation ? OperationResult.Created : OperationResult.Updated,
            resource, null, null);
    }
    
    internal ControlPlaneOperationResult<RoleDefinitionResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        RoleDefinitionIdentifier roleDefinitionIdentifier)
    {
        logger.LogDebug(nameof(AuthorizationControlPlane), nameof(Get),
            "Looking for role `{0}` in subscription `{1}`.", roleDefinitionIdentifier, subscriptionIdentifier);
        
        var resource = subscriptionAuthorizationProvider.GetAs<RoleDefinitionResource>(subscriptionIdentifier, null, roleDefinitionIdentifier.Value);
        if (resource != null)
            return new ControlPlaneOperationResult<RoleDefinitionResource>(OperationResult.Success, resource, null,
                null);

        logger.LogDebug(nameof(AuthorizationControlPlane), nameof(Get),
            "Looking for role `{0}` in subscription `{1}` failed when looking for an exact match. Falling back to filtering using `roleName`.",
            roleDefinitionIdentifier, subscriptionIdentifier);
        
        // Here's the thing - a role definition can be found be either its `name` - which is a GUID
        // or `roleName` - which is a human-friendly string. If a role definition is created via SDK,
        // then a user has control over the identifier used there. For Azure CLI however, the
        // identifier is generated and cannot be provided explicitly. Topaz saves a role definition
        // using that generated GUID, but a user may query the emulator using `roleName`.
        var availableDefinitions =
            ListRoleDefinitionsBySubscription(subscriptionIdentifier);

        resource = availableDefinitions.Resource?.FirstOrDefault(definition =>
            definition.Properties.RoleName == roleDefinitionIdentifier.Value ||
            definition.Id.Contains(roleDefinitionIdentifier.Value));

        return resource == null
            ? new ControlPlaneOperationResult<RoleDefinitionResource>(OperationResult.NotFound, null,
                string.Format(RoleDefinitionNotFoundMessageTemplate, roleDefinitionIdentifier),
                RoleDefinitionNotFoundMessageCode)
            : new ControlPlaneOperationResult<RoleDefinitionResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<RoleDefinitionResource?> Delete(SubscriptionIdentifier subscriptionIdentifier, RoleDefinitionIdentifier roleDefinitionIdentifier)
    {
        var resource = subscriptionAuthorizationProvider.GetAs<RoleDefinitionResource>(subscriptionIdentifier, null, roleDefinitionIdentifier.Value);
        if (resource == null || !resource.IsInSubscription(subscriptionIdentifier))
        {
            return new ControlPlaneOperationResult<RoleDefinitionResource?>(OperationResult.NotFound, null,
                string.Format(RoleDefinitionNotFoundMessageTemplate, roleDefinitionIdentifier),
                RoleDefinitionNotFoundMessageCode);
        }

        subscriptionAuthorizationProvider.Delete(subscriptionIdentifier, null, roleDefinitionIdentifier.Value);
        
        return new ControlPlaneOperationResult<RoleDefinitionResource?>(OperationResult.Success, resource, null, null);
    }
    
    public ControlPlaneOperationResult<RoleAssignmentResource?> Delete(SubscriptionIdentifier subscriptionIdentifier, RoleAssignmentName roleAssignmentName)
    {
        var resource = subscriptionAuthorizationProvider.GetAs<RoleAssignmentResource>(subscriptionIdentifier, null, roleAssignmentName.Value.ToString());
        if (resource == null || !resource.IsInSubscription(subscriptionIdentifier))
        {
            return new ControlPlaneOperationResult<RoleAssignmentResource?>(OperationResult.NotFound, null,
                string.Format(RoleAssignmentNotFoundMessageTemplate, roleAssignmentName),
                RoleAssignmentNotFoundMessageCode);
        }

        subscriptionAuthorizationProvider.Delete(subscriptionIdentifier, null, roleAssignmentName.Value.ToString());
        
        return new ControlPlaneOperationResult<RoleAssignmentResource?>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<RoleAssignmentResource?> CreateOrUpdateRoleAssignment(
        SubscriptionIdentifier subscriptionIdentifier, RoleAssignmentName roleAssignmentName,
        CreateOrUpdateRoleAssignmentRequest request)
    {
        var subscriptionOperation = subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<RoleAssignmentResource?>(OperationResult.Failed, null,
                subscriptionOperation.Reason,
                subscriptionOperation.Code);
        }

        RoleAssignmentResource resource;
        var roleDefinitionOperation = Get(subscriptionIdentifier, roleAssignmentName);
        var createOperation = roleDefinitionOperation.Result == OperationResult.NotFound;
        if (createOperation)
        {
            var properties = RoleAssignmentResourceProperties.FromRequest(request);
            properties.Scope = $"/subscriptions/{subscriptionIdentifier.Value}";
            properties.CreatedOn = DateTimeOffset.UtcNow;
            properties.UpdatedOn = DateTimeOffset.UtcNow;
            
            resource = new RoleAssignmentResource(subscriptionIdentifier, roleAssignmentName.Value.ToString(), properties);
        }
        else
        {
            RoleAssignmentResourceProperties.UpdateFromRequest(roleDefinitionOperation.Resource!, request);
            resource = roleDefinitionOperation.Resource!;
        }
        
        subscriptionAuthorizationProvider.CreateOrUpdate(subscriptionIdentifier, null, roleAssignmentName.Value.ToString(), resource, createOperation);
        
        return new ControlPlaneOperationResult<RoleAssignmentResource?>(
            createOperation ? OperationResult.Created : OperationResult.Updated,
            resource, null, null);
    }
    
    internal ControlPlaneOperationResult<RoleAssignmentResource> Get(SubscriptionIdentifier subscriptionIdentifier,
        RoleAssignmentName roleAssignmentName)
    {
        var resource = subscriptionAuthorizationProvider.GetAs<RoleAssignmentResource>(subscriptionIdentifier, null, roleAssignmentName.Value.ToString());
        return resource == null
            ? new ControlPlaneOperationResult<RoleAssignmentResource>(OperationResult.NotFound, null,
                string.Format(RoleDefinitionNotFoundMessageTemplate, roleAssignmentName),
                RoleDefinitionNotFoundMessageCode)
            : new ControlPlaneOperationResult<RoleAssignmentResource>(OperationResult.Success, resource, null, null);
    }

    public ControlPlaneOperationResult<RoleDefinitionResource[]> ListRoleDefinitionsBySubscription(SubscriptionIdentifier subscriptionIdentifier, string? roleName = null)
    {
        var resources =
            subscriptionAuthorizationProvider.ListAs<RoleDefinitionResource>(subscriptionIdentifier, null, null, 6);
        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier));
        
        // There are also built-in roles which should be included for every request
        // listing role definitions
        var builtInRoles = subscriptionAuthorizationProvider.ListBuiltInRoles(subscriptionIdentifier);
        filteredResources = filteredResources.Concat(builtInRoles);
        
        // If filter was provided, use it to limit roles
        if (roleName != null)
        {
            filteredResources = filteredResources.Where(resource =>
                resource.Name == roleName || resource.Properties.RoleName == roleName);
        }
        
        return new ControlPlaneOperationResult<RoleDefinitionResource[]>(OperationResult.Success, filteredResources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<RoleAssignmentResource[]> ListRoleAssignmentsBySubscription(SubscriptionIdentifier subscriptionIdentifier, string? roleName = null)
    {
        var resources =
            subscriptionAuthorizationProvider.ListAs<RoleAssignmentResource>(subscriptionIdentifier, null, null, 6);
        var filteredResources = resources.Where(resource => resource.IsInSubscription(subscriptionIdentifier));
        
        return new ControlPlaneOperationResult<RoleAssignmentResource[]>(OperationResult.Success, filteredResources.ToArray(), null, null);
    }
    
    public ControlPlaneOperationResult<RoleAssignmentResource[]> ListSubscriptionRoleAssignmentsByEntraObject(SubscriptionIdentifier subscriptionIdentifier, string objectId)
    {
        var resources =
            subscriptionAuthorizationProvider.ListAs<RoleAssignmentResource>(subscriptionIdentifier, null, null, 6);
        var filteredResources = resources.Where(resource =>
            resource.IsInSubscription(subscriptionIdentifier) && resource.Properties.PrincipalId == objectId);
        
        return new ControlPlaneOperationResult<RoleAssignmentResource[]>(OperationResult.Success, filteredResources.ToArray(), null, null);
    }
}
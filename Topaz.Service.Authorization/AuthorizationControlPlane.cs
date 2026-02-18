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

    public static AuthorizationControlPlane New(ITopazLogger logger) => new(
        new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)),
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
        var resource = subscriptionAuthorizationProvider.GetAs<RoleDefinitionResource>(subscriptionIdentifier, null, roleDefinitionIdentifier.Value);
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
}
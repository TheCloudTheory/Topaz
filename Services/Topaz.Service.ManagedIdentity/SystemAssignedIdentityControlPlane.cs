using Topaz.Service.Entra;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Planes;
using Topaz.Service.ManagedIdentity.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity;

internal sealed class SystemAssignedIdentityControlPlane(
    SystemAssignedIdentityResourceProvider provider,
    ServicePrincipalDataPlane servicePrincipalDataPlane,
    ITopazLogger logger)
{
    public static SystemAssignedIdentityControlPlane New(ITopazLogger logger) =>
        new(new SystemAssignedIdentityResourceProvider(logger),
            ServicePrincipalDataPlane.New(logger),
            logger);

    public ControlPlaneOperationResult<SystemAssignedIdentityResource> CreateOrUpdate(string parentResourceId)
    {
        var (subscriptionIdentifier, resourceGroupIdentifier, encodedId) = ParseResourceId(parentResourceId);

        var existing = provider.GetAs<SystemAssignedIdentityResource>(
            subscriptionIdentifier, resourceGroupIdentifier, encodedId);

        if (existing != null)
        {
            logger.LogDebug(nameof(SystemAssignedIdentityControlPlane), nameof(CreateOrUpdate),
                "System-assigned identity already exists for resource `{0}`.", parentResourceId);
            return new ControlPlaneOperationResult<SystemAssignedIdentityResource>(
                OperationResult.Success, existing, null, null);
        }

        var createSpOperation = servicePrincipalDataPlane.Create(
            CreateServicePrincipalRequest.Generate(parentResourceId));

        if (createSpOperation.Result != OperationResult.Created)
        {
            return new ControlPlaneOperationResult<SystemAssignedIdentityResource>(
                OperationResult.Failed, null, createSpOperation.Reason, createSpOperation.Code);
        }

        var sp = createSpOperation.Resource!;
        var identity = new SystemAssignedIdentityResource(parentResourceId, sp.Id, EntraService.TenantId);
        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, encodedId, identity);

        logger.LogDebug(nameof(SystemAssignedIdentityControlPlane), nameof(CreateOrUpdate),
            "Created system-assigned identity for resource `{0}`.", parentResourceId);

        return new ControlPlaneOperationResult<SystemAssignedIdentityResource>(
            OperationResult.Created, identity, null, null);
    }

    public ControlPlaneOperationResult<SystemAssignedIdentityResource> Get(string parentResourceId)
    {
        var (subscriptionIdentifier, resourceGroupIdentifier, encodedId) = ParseResourceId(parentResourceId);

        var existing = provider.GetAs<SystemAssignedIdentityResource>(
            subscriptionIdentifier, resourceGroupIdentifier, encodedId);

        if (existing == null)
        {
            logger.LogDebug(nameof(SystemAssignedIdentityControlPlane), nameof(Get),
                "System-assigned identity not found for resource `{0}`.", parentResourceId);
            return new ControlPlaneOperationResult<SystemAssignedIdentityResource>(
                OperationResult.NotFound, null, $"System-assigned identity not found for resource '{parentResourceId}'.", "IdentityNotFound");
        }

        logger.LogDebug(nameof(SystemAssignedIdentityControlPlane), nameof(Get),
            "Found system-assigned identity for resource `{0}`.", parentResourceId);
        return new ControlPlaneOperationResult<SystemAssignedIdentityResource>(
            OperationResult.Success, existing, null, null);
    }

    /// <summary>
    /// Parses an ARM resource ID into its subscription, resource group, and a filesystem-safe
    /// encoded key covering the provider/type/name tail of the path.
    /// Example: /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Compute/virtualMachines/{name}
    ///   → subscriptionId={sub}, resourceGroup={rg}, encodedId=providers_microsoft.compute_virtualmachines_{name}
    /// </summary>
    private static (SubscriptionIdentifier, ResourceGroupIdentifier, string) ParseResourceId(string resourceId)
    {
        var parts = resourceId.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // parts[0]=subscriptions, [1]={sub}, [2]=resourceGroups, [3]={rg}, [4..]=rest
        var subscriptionId = SubscriptionIdentifier.From(parts[1]);
        var resourceGroup = ResourceGroupIdentifier.From(parts[3]);
        var tailParts = parts[4..];
        var encodedId = string.Join("_", tailParts).ToLowerInvariant();
        return (subscriptionId, resourceGroup, encodedId);
    }
}


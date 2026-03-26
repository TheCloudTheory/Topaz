using JetBrains.Annotations;
using Topaz.Service.Authorization.Models.Requests;

namespace Topaz.Service.Authorization.Models;

[UsedImplicitly]
internal sealed class RoleAssignmentResourceProperties
{
    public string? RoleDefinitionId { get; set; }
    public string? PrincipalId { get; set; }
    public string? PrincipalType { get; set; }
    public string? Scope { get; set; }
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? ConditionVersion { get; set; }
    public string? DelegatedManagedIdentityResourceId { get; set; }

    public DateTimeOffset? CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public string? UpdatedBy { get; set; }

    public static RoleAssignmentResourceProperties FromRequest(CreateOrUpdateRoleAssignmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Properties == null) throw new ArgumentNullException(nameof(request.Properties));

        return new RoleAssignmentResourceProperties
        {
            RoleDefinitionId = request.Properties.RoleDefinitionId,
            PrincipalId = request.Properties.PrincipalId,
            PrincipalType = request.Properties.PrincipalType,
            Scope = request.Properties.Scope,
            Description = request.Properties.Description,
            Condition = request.Properties.Condition,
            ConditionVersion = request.Properties.ConditionVersion,
            DelegatedManagedIdentityResourceId = request.Properties.DelegatedManagedIdentityResourceId
        };
    }

    public static void UpdateFromRequest(RoleAssignmentResource resource, CreateOrUpdateRoleAssignmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(request);
        if (request.Properties == null) throw new ArgumentNullException(nameof(request.Properties));

        var properties = resource.Properties;
        var req = request.Properties;

        if (req.RoleDefinitionId != null)
            properties.RoleDefinitionId = req.RoleDefinitionId;

        if (req.PrincipalId != null)
            properties.PrincipalId = req.PrincipalId;

        if (req.PrincipalType != null)
            properties.PrincipalType = req.PrincipalType;

        if (req.Scope != null)
            properties.Scope = req.Scope;

        if (req.Description != null)
            properties.Description = req.Description;

        if (req.Condition != null)
            properties.Condition = req.Condition;

        if (req.ConditionVersion != null)
            properties.ConditionVersion = req.ConditionVersion;

        if (req.DelegatedManagedIdentityResourceId != null)
            properties.DelegatedManagedIdentityResourceId = req.DelegatedManagedIdentityResourceId;

        properties.UpdatedOn = DateTimeOffset.UtcNow;
    }
}

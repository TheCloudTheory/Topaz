using JetBrains.Annotations;
using Topaz.Service.Authorization.Models.Requests;

namespace Topaz.Service.Authorization.Models;

[UsedImplicitly]
internal sealed class RoleDefinitionResourceProperties
{
	public string? RoleName { get; set; }
	public string? Description { get; set; }
	public string? Type { get; set; }
	public string[]? AssignableScopes { get; set; }
	public RoleDefinition.Permission[]? Permissions { get; set; }
	public DateTimeOffset? CreatedOn { get; set; }
	public string? CreatedBy { get; set; }
	public DateTimeOffset? UpdatedOn { get; set; }
	public string? UpdatedBy { get; set; }

	public static RoleDefinitionResourceProperties FromRequest(CreateOrUpdateRoleDefinitionRequest request)
	{
        ArgumentNullException.ThrowIfNull(request);
        if (request.Properties == null) throw new ArgumentNullException(nameof(request.Properties));

        return new RoleDefinitionResourceProperties
        {
	        RoleName = request.Properties.RoleName,
	        Description = request.Properties.Description,
	        Type = request.Properties.RoleType,
	        AssignableScopes = request.Properties.AssignableScopes.ToArray(),
	        Permissions = request.Properties.Permissions.Select(p => new RoleDefinition.Permission
	        {
		        Actions = p.Actions.ToArray(),
		        NotActions = p.NotActions.ToArray(),
		        DataActions = p.DataActions.ToArray(),
		        NotDataActions = p.NotDataActions.ToArray()
	        }).ToArray()
        };
	}

	public static void UpdateFromRequest(RoleDefinitionResource resource, CreateOrUpdateRoleDefinitionRequest request)
	{
		ArgumentNullException.ThrowIfNull(resource);
		ArgumentNullException.ThrowIfNull(request);
		if (request.Properties == null) throw new ArgumentNullException(nameof(request.Properties));

		var properties = resource.Properties;
		var req = request.Properties;

		if (req.RoleName != null)
			properties.RoleName = req.RoleName;

		if (req.Description != null)
			properties.Description = req.Description;

		if (req.RoleType != null)
			properties.Type = req.RoleType;

		properties.AssignableScopes = req.AssignableScopes.ToArray();

		properties.Permissions = req.Permissions.Select(p => new RoleDefinition.Permission
		{
			Actions = p.Actions?.ToArray(),
			NotActions = p.NotActions?.ToArray(),
			DataActions = p.DataActions?.ToArray(),
			NotDataActions = p.NotDataActions?.ToArray()
		}).ToArray();

		properties.UpdatedOn = DateTimeOffset.UtcNow;
	}
}
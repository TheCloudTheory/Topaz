namespace Topaz.Service.Authorization.Models.Requests;

public sealed class CreateOrUpdateRoleAssignmentRequest
{
	public RoleAssignmentProperties Properties { get; init; } = new();
}

public sealed class RoleAssignmentProperties
{
	public string? RoleDefinitionId { get; set; }
	public string? PrincipalId { get; set; }
	public string? PrincipalType { get; set; }
	public string? Description { get; set; }
	public string? Condition { get; set; }
	public string? ConditionVersion { get; set; }
	public string? DelegatedManagedIdentityResourceId { get; set; }
	public string? Scope { get; set; }
}
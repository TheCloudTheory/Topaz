namespace Topaz.Service.Authorization.Models.Requests;

public sealed class CreateOrUpdateRoleDefinitionRequest
{
	public RoleDefinitionProperties Properties { get; init; } = new();
}

public sealed class RoleDefinitionProperties
{
	public string? RoleName { get; set; }
	public string? Description { get; set; }
	public List<RoleDefinitionPermissionModel> Permissions { get; set; } = [];
	public List<string> AssignableScopes { get; set; } = [];
	public string? RoleType { get; set; }
}

public sealed class RoleDefinitionPermissionModel
{
	public List<string> Actions { get; set; } = [];
	public List<string> NotActions { get; set; } = [];
	public List<string> DataActions { get; set; } = [];
	public List<string> NotDataActions { get; set; } = [];
}
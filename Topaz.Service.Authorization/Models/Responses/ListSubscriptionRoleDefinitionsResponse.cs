using System.Text.Json;

namespace Topaz.Service.Authorization.Models.Responses;

internal sealed class ListSubscriptionRoleDefinitionsResponse
{
	public RoleDefinition[]? Value { get; init; }
	public string? NextLink { get; init; }

	public record RoleDefinition
	{
		public string? Id { get; init; }
		public string? Name { get; init; }
		public string? Type { get; init; }
		public RoleDefinitionProperties? Properties { get; init; }

		public static RoleDefinition From(RoleDefinitionResource resource)
		{
			return new RoleDefinition
			{
				Id = resource.Id,
				Name = resource.Name,
				Type = resource.Type,
				Properties = new RoleDefinitionProperties
				{
					RoleName = resource.Properties.RoleName,
					Description = resource.Properties.Description,
					RoleType = resource.Properties.Type,
					AssignableScopes = resource.Properties.AssignableScopes,
					Permissions = resource.Properties.Permissions?.Select(p => new Permission
					{
						Actions = p.Actions,
						NotActions = p.NotActions,
						DataActions = p.DataActions,
						NotDataActions = p.NotDataActions
					}).ToArray(),
					CreatedOn = resource.Properties.CreatedOn,
					CreatedBy = resource.Properties.CreatedBy,
					UpdatedOn = resource.Properties.UpdatedOn,
					UpdatedBy = resource.Properties.UpdatedBy
				}
			};
		}
	}

	public record RoleDefinitionProperties
	{
		public string? RoleName { get; init; }
		public string? Description { get; init; }
		public string? RoleType { get; init; }
		public string[]? AssignableScopes { get; init; }
		public Permission[]? Permissions { get; init; }
		public DateTimeOffset? CreatedOn { get; init; }
		public string? CreatedBy { get; init; }
		public DateTimeOffset? UpdatedOn { get; init; }
		public string? UpdatedBy { get; init; }
	}

	public record Permission
	{
		public string[]? Actions { get; init; }
		public string[]? NotActions { get; init; }
		public string[]? DataActions { get; init; }
		public string[]? NotDataActions { get; init; }
	}

	public override string ToString() => JsonSerializer.Serialize(this, Topaz.Shared.GlobalSettings.JsonOptions);
}
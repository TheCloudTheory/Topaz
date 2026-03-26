using System.Text.Json;

namespace Topaz.Service.Authorization.Models.Responses;

internal sealed class ListSubscriptionRoleAssignmentsResponse
{
    public RoleAssignment[]? Value { get; init; }
    public string? NextLink { get; init; }

    public record RoleAssignment
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Type { get; init; }
        public RoleAssignmentProperties? Properties { get; init; }

        public static RoleAssignment From(RoleAssignmentResource resource)
        {
            return new RoleAssignment
            {
                Id = resource.Id,
                Name = resource.Name,
                Type = resource.Type,
                Properties = new RoleAssignmentProperties
                {
                    RoleDefinitionId = resource.Properties.RoleDefinitionId,
                    PrincipalId = resource.Properties.PrincipalId,
                    PrincipalType = resource.Properties.PrincipalType,
                    Scope = resource.Properties.Scope,
                    Condition = resource.Properties.Condition,
                    ConditionVersion = resource.Properties.ConditionVersion,
                    CreatedOn = resource.Properties.CreatedOn,
                    CreatedBy = resource.Properties.CreatedBy,
                    UpdatedOn = resource.Properties.UpdatedOn,
                    UpdatedBy = resource.Properties.UpdatedBy
                }
            };
        }
    }

    public record RoleAssignmentProperties
    {
        public string? RoleDefinitionId { get; init; }
        public string? PrincipalId { get; init; }
        public string? PrincipalType { get; init; }
        public string? Scope { get; init; }
        public string? Condition { get; init; }
        public string? ConditionVersion { get; init; }
        public DateTimeOffset? CreatedOn { get; init; }
        public string? CreatedBy { get; init; }
        public DateTimeOffset? UpdatedOn { get; init; }
        public string? UpdatedBy { get; init; }
    }

    public override string ToString() => JsonSerializer.Serialize(this, Topaz.Shared.GlobalSettings.JsonOptions);
}
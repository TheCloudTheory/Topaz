using System.Text.Json.Serialization;

namespace Topaz.Portal.Models.Rbac;

public sealed class ListRoleAssignmentsResponse
{
    [JsonPropertyName("value")]
    public RoleAssignmentDto[] Value { get; init; } = [];
    
    // Used by the UI to request the next page.
    public string? ContinuationToken { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }
}

public sealed class RoleAssignmentDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("properties")]
    public RoleAssignmentPropertiesDto? Properties { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }
}

public sealed class RoleAssignmentPropertiesDto
{
    [JsonPropertyName("roleDefinitionId")]
    public string? RoleDefinitionId { get; init; }

    [JsonPropertyName("principalId")]
    public string? PrincipalId { get; init; }

    [JsonPropertyName("principalType")]
    public string? PrincipalType { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }
}
using System.Text.Json.Serialization;

namespace Topaz.Portal.Models.Rbac;

public sealed class ListRoleDefinitionsResponse
{
    [JsonPropertyName("value")]
    public RoleDefinitionDto[] Value { get; init; } = [];
    
    // Used by the UI to request the next page.
    // This is not an ARM field we receive; it’s computed by the client.
    public string? ContinuationToken { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }
}

public sealed class RoleDefinitionDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("properties")]
    public RoleDefinitionPropertiesDto? Properties { get; init; }

    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }
}

public sealed class RoleDefinitionPropertiesDto
{
    [JsonPropertyName("roleName")]
    public string? RoleName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    // Often "BuiltInRole" or "CustomRole"
    [JsonPropertyName("roleType")]
    public string? RoleType { get; init; }

    [JsonIgnore]
    public bool IsBuiltIn => string.Equals(RoleType, "BuiltInRole", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsCustom => string.Equals(RoleType, "CustomRole", StringComparison.OrdinalIgnoreCase);

    [JsonExtensionData]
    public IDictionary<string, object?>? AdditionalData { get; init; }
}
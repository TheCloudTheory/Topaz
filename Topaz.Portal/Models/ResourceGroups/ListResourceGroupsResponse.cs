namespace Topaz.Portal.Models.ResourceGroups;

public sealed class ListResourceGroupsResponse
{
    public ResourceGroupDto[] Value { get; init; } = [];
}

public sealed class ResourceGroupDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Location { get; init; }
    public string? Type { get; init; }
}
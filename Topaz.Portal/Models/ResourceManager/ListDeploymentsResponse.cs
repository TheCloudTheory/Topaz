namespace Topaz.Portal.Models.ResourceManager;

public sealed class ListDeploymentsResponse
{
    public DeploymentDto[] Value { get; init; } = [];
    public string? NextLink { get; init; }
}

public sealed class DeploymentDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public string? Location { get; init; }
    public DeploymentPropertiesDto? Properties { get; init; }
}

public sealed class DeploymentPropertiesDto
{
    public string? ProvisioningState { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
    public string? Mode { get; init; }
}
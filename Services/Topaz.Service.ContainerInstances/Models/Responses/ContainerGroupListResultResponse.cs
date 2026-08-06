using Topaz.Service.Shared;

namespace Topaz.Service.ContainerInstances.Models.Responses;

internal sealed class ContainerGroupListResultResponse : TopazApiModel
{
    public string? NextLink { get; init; }
    public ContainerInstancesServiceResource[]? Value { get; init; }

    public static ContainerGroupListResultResponse From(ContainerInstancesServiceResource[] containerGroups)
    {
        return new ContainerGroupListResultResponse
        {
            NextLink = string.Empty,
            Value = containerGroups
        };
    }
}
using Topaz.Service.Shared;

namespace Topaz.Service.ContainerInstances.Models.Responses;

internal sealed class ContainerLogsResponse : TopazApiModel
{
    public string? Content { get; set; }

    public static ContainerLogsResponse From(string logs)
    {
        return new ContainerLogsResponse { Content = logs };
    }
}
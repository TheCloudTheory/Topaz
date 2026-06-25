using Topaz.FinOps.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.FinOps;

public sealed class FinOpsService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => string.Empty;
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "finops";

    public string Name => "FinOps";
    public bool IsTopazService => true;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new GetEstimatedCostsEndpoint(logger)
    ];
}

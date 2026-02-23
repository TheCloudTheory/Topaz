using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights;

public sealed class InsightsService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".insights");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "insights";
    public string Name => "Insights";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new InsightsServiceEndpoint()
    ];

    public void Bootstrap()
    {
    }
}
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceTemplate;

public sealed class ServiceTemplateService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".servicetemplate");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "servicetemplate";

    public string Name => "Azure ServiceTemplate";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        // TODO: register your endpoints here
    ];
}

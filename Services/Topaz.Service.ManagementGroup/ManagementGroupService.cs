using Topaz.Service.ManagementGroup.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup;

public sealed class ManagementGroupService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => ".management-group";
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "managementgroup";

    public string Name => "Management Group";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateOrUpdateManagementGroupEndpoint(logger),
        new GetManagementGroupEndpoint(logger),
        new DeleteManagementGroupEndpoint(logger),
        new ListManagementGroupsEndpoint(logger),
        new UpdateManagementGroupEndpoint(logger),
    ];

    public void Bootstrap()
    {
    }
}

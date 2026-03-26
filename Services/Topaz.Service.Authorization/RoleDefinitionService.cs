using Topaz.EventPipeline;
using Topaz.Service.Authorization.Endpoints.RoleDefinitions;
using Topaz.Service.Shared;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public sealed class RoleDefinitionService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(SubscriptionService.LocalDirectoryPath, ".role-definition");

    public static IReadOnlyCollection<string> Subresources => [];

    public static string UniqueName => "role-definition";
    public string Name => "Subscription Role Definition";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateUpdateRoleDefinitionEndpoint(eventPipeline, logger),
        new ListRoleDefinitionsEndpoint(eventPipeline, logger),
        new GetRoleDefinitionEndpoint(eventPipeline, logger),
        new DeleteRoleDefinitionEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}
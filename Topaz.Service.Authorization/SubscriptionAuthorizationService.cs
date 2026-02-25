using Topaz.EventPipeline;
using Topaz.Service.Authorization.Endpoints;
using Topaz.Service.Authorization.Endpoints.RoleAssignments;
using Topaz.Service.Authorization.Endpoints.RoleDefinitions;
using Topaz.Service.Shared;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public sealed class SubscriptionAuthorizationService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(SubscriptionService.LocalDirectoryPath, ".authorization");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "subscription-authorization";
    public string Name => "Subscription Authorization";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new CreateUpdateRoleDefinitionEndpoint(eventPipeline, logger),
        new ListRoleDefinitionsEndpoint(eventPipeline, logger),
        new GetRoleDefinitionEndpoint(eventPipeline, logger),
        new DeleteRoleDefinitionEndpoint(eventPipeline, logger),
        new CreateUpdateRoleAssignmentEndpoint(eventPipeline, logger),
        new ListRoleAssignmentsEndpoint(eventPipeline, logger),
        new GetRoleAssignmentEndpoint(eventPipeline, logger),
        new DeleteRoleAssignmentEndpoint(eventPipeline, logger),
    ];

    public void Bootstrap()
    {
    }
}

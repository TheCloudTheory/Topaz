using Topaz.Service.Authorization.Endpoints;
using Topaz.Service.Authorization.Endpoints.RoleAssignments;
using Topaz.Service.Authorization.Endpoints.RoleDefinitions;
using Topaz.Service.Shared;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public sealed class SubscriptionAuthorizationService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(SubscriptionService.LocalDirectoryPath, ".authorization");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "subscription-authorization";
    public string Name => "Subscription Authorization";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new CreateUpdateRoleDefinitionEndpoint(logger),
        new ListRoleDefinitionsEndpoint(logger),
        new GetRoleDefinitionEndpoint(logger),
        new DeleteRoleDefinitionEndpoint(logger),
        new CreateUpdateRoleDefinitionAssignmentEndpoint(logger),
        new ListRoleAssignmentsEndpoint(logger),
        new GetRoleAssignmentEndpoint(logger),
        new DeleteRoleAssignmentEndpoint(logger),
    ];

    public void Bootstrap()
    {
    }
}

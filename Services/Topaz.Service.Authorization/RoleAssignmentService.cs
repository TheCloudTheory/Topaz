using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
using Topaz.Identity;
using Topaz.Service.Authorization.Domain;
using Topaz.Service.Authorization.Endpoints.RoleAssignments;
using Topaz.Service.Authorization.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.Authorization;

public sealed class RoleAssignmentService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => false;
    public static string LocalDirectoryPath => Path.Combine(SubscriptionService.LocalDirectoryPath, ".role-assignment");

    public static IReadOnlyCollection<string> Subresources => [];

    public static string UniqueName => "subscription-role-assignment";
    public string Name => "Subscription Role Assignment";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new CreateUpdateRoleAssignmentEndpoint(eventPipeline, logger),
        new ListRoleAssignmentsEndpoint(eventPipeline, logger),
        new GetRoleAssignmentEndpoint(eventPipeline, logger),
        new DeleteRoleAssignmentEndpoint(eventPipeline, logger),
    ];

    public void Bootstrap()
    {
        var controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);

        eventPipeline.RegisterHandler<SubscriptionCreatedEventData>(
            SubscriptionCreatedEvent.EventName,
            data => controlPlane.CreateOrUpdateRoleAssignment(SubscriptionIdentifier.From(data!.SubscriptionId),
                RoleAssignmentName.From(Guid.Empty),
                new CreateOrUpdateRoleAssignmentRequest
                {
                    Properties = new RoleAssignmentProperties
                    {
                        PrincipalId = Globals.GlobalAdminId,
                        RoleDefinitionId = "8e3af657-a8ff-443c-a75c-2fe8c4bcb635",
                        Scope = $"/subscriptions/{data!.SubscriptionId}"
                    }
                }));
    }
}
using System.Text.Json.Nodes;
using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
using Topaz.Service.ManagementGroup.Endpoints;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup;

public sealed class ManagementGroupService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
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
        new AssociateSubscriptionEndpoint(logger),
        new DisassociateSubscriptionEndpoint(logger),
        new GetSubscriptionUnderManagementGroupEndpoint(logger),
        new GetEntitiesEndpoint(logger),
        new GetDescendantsEndpoint(logger),
        new CreateOrUpdateHierarchySettingsEndpoint(logger),
        new GetHierarchySettingsEndpoint(logger),
        new ListHierarchySettingsEndpoint(logger),
        new DeleteHierarchySettingsEndpoint(logger),
        new UpdateHierarchySettingsEndpoint(logger),
    ];

    public void Bootstrap()
    {
        var mgControlPlane = ManagementGroupControlPlane.New(logger);
        var mgProvider = new ManagementGroupResourceProvider(logger);
        var subProvider = new SubscriptionResourceProvider(logger);

        eventPipeline.RegisterHandler<TenantInitializedEventData>(
            TenantInitializedEvent.EventName,
            data =>
            {
                if (data == null) return;

                // Idempotently create the root management group.
                if (mgControlPlane.Get(data.TenantId).Result == OperationResult.NotFound)
                {
                    mgControlPlane.CreateOrUpdate(data.TenantId,
                        new CreateOrUpdateManagementGroupRequest
                        {
                            Properties = new CreateManagementGroupProperties
                            {
                                DisplayName = "Tenant Root Group"
                            }
                        });

                    logger.LogDebug(nameof(ManagementGroupService), nameof(Bootstrap),
                        "Root management group '{0}' created.", data.TenantId);
                }

                // Associate any existing subscriptions not yet placed in any management group.
                var assignedSubscriptionIds = mgProvider
                    .ListAllSubscriptionAssociations()
                    .Select(s => s.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var json in subProvider.List(null, null))
                {
                    var subId = JsonNode.Parse(json)?["subscriptionId"]?.GetValue<string>();
                    if (subId == null || assignedSubscriptionIds.Contains(subId)) continue;

                    mgControlPlane.AssociateSubscription(data.TenantId, subId);
                    logger.LogDebug(nameof(ManagementGroupService), nameof(Bootstrap),
                        "Associated existing subscription '{0}' with root management group.", subId);
                }
            });

        // Auto-place new subscriptions under the root management group.
        eventPipeline.RegisterHandler<SubscriptionCreatedEventData>(
            SubscriptionCreatedEvent.EventName,
            data =>
            {
                if (data == null) return;

                var alreadyAssigned = mgProvider
                    .ListAllSubscriptionAssociations()
                    .Any(s => string.Equals(s.Name, data.SubscriptionId, StringComparison.OrdinalIgnoreCase));

                if (alreadyAssigned) return;

                var rootGroupId = GlobalSettings.DefaultTenantId;
                if (mgControlPlane.Get(rootGroupId).Result == OperationResult.NotFound) return;

                mgControlPlane.AssociateSubscription(rootGroupId, data.SubscriptionId);
                logger.LogDebug(nameof(ManagementGroupService), nameof(Bootstrap),
                    "Associated new subscription '{0}' with root management group.", data.SubscriptionId);
            });
    }
}

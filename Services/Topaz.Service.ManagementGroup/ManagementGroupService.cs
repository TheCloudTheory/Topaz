using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
using Topaz.Service.ManagementGroup.Endpoints;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.Shared;
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
        var controlPlane = ManagementGroupControlPlane.New(logger);

        eventPipeline.RegisterHandler<TenantInitializedEventData>(
            TenantInitializedEvent.EventName,
            data =>
            {
                if (data == null) return;
                var existing = controlPlane.Get(data.TenantId);
                if (existing.Result != OperationResult.NotFound) return;

                controlPlane.CreateOrUpdate(data.TenantId,
                    new CreateOrUpdateManagementGroupRequest
                    {
                        Properties = new CreateManagementGroupProperties
                        {
                            DisplayName = "Tenant Root Group"
                        }
                    });

                logger.LogDebug(nameof(ManagementGroupService), nameof(Bootstrap),
                    "Root management group '{0}' created.", data.TenantId);
            });
    }
}

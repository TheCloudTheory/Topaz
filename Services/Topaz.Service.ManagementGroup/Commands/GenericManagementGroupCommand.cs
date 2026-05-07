using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagementGroup.Commands;

public sealed class GenericManagementGroupCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("management-group", mg =>
        {
            mg.AddCommand<CreateManagementGroupCommand>("create");
            mg.AddCommand<ShowManagementGroupCommand>("show");
            mg.AddCommand<ListManagementGroupsCommand>("list");
            mg.AddCommand<UpdateManagementGroupCommand>("update");
            mg.AddCommand<DeleteManagementGroupCommand>("delete");

            mg.AddBranch("subscription", sub =>
            {
                sub.AddCommand<AddManagementGroupSubscriptionCommand>("add");
                sub.AddCommand<RemoveManagementGroupSubscriptionCommand>("remove");
                sub.AddCommand<ShowManagementGroupSubscriptionCommand>("show");
            });

            mg.AddBranch("descendants", descendants =>
            {
                descendants.AddCommand<ListManagementGroupDescendantsCommand>("list");
            });

            mg.AddBranch("hierarchy-settings", hs =>
            {
                hs.AddCommand<CreateOrUpdateHierarchySettingsCommand>("create-or-update");
                hs.AddCommand<ShowHierarchySettingsCommand>("show");
                hs.AddCommand<ListHierarchySettingsCommand>("list");
                hs.AddCommand<UpdateHierarchySettingsCommand>("update");
                hs.AddCommand<DeleteHierarchySettingsCommand>("delete");
            });
        });
    }
}

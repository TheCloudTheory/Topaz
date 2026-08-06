using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerInstances.Commands;

public sealed class GenericContainerInstancesCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("containerinstances", aci =>
        {
            aci.AddBranch("group", group =>
            {
                group.AddCommand<CreateOrUpdateContainerGroupCommand>("create");
                group.AddCommand<GetContainerGroupCommand>("get");
                group.AddCommand<DeleteContainerGroupCommand>("delete");
                group.AddCommand<ListContainerGroupsByResourceGroupCommand>("list");
                group.AddCommand<ListContainerGroupsCommand>("list-all");
                group.AddCommand<StartContainerGroupCommand>("start");
                group.AddCommand<StopContainerGroupCommand>("stop");
                group.AddCommand<RestartContainerGroupCommand>("restart");
                group.AddCommand<UpdateContainerGroupCommand>("update");
            });
        });
    }
}

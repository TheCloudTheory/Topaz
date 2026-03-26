using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ResourceGroup.Commands;

public sealed class GenericResourceGroupCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("group", group => {
            group.AddCommand<CreateResourceGroupCommand>("create");
            group.AddCommand<DeleteResourceGroupCommand>("delete");
            group.AddCommand<ListResourceGroupCommand>("list");
            group.AddCommand<ShowResourceGroupCommand>("show");
        });
    }
}
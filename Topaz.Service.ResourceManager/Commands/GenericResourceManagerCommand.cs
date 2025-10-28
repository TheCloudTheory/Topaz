using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ResourceManager.Commands;

public sealed class GenericResourceManagerCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("deployment", branch =>
        {
            branch.AddBranch("group", groupDeployment =>
            {
                groupDeployment.AddCommand<CreateGroupDeploymentCommand>("create");
                groupDeployment.AddCommand<ListGroupDeploymentCommand>("list");
            });
        });
    }
}
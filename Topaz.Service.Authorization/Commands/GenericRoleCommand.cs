using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.Authorization.Commands;

public class GenericRoleCommand: IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("role", group =>
        {
            group.AddBranch("assignment", assignment =>
            {
                assignment.AddCommand<CreateRoleAssignmentCommand>("create");
            });
        });
    }
}
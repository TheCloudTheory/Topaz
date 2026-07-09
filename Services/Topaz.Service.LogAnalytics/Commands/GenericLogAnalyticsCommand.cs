using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.LogAnalytics.Commands;

public sealed class GenericLogAnalyticsCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("loganalytics", la =>
        {
            la.AddCommand<CreateWorkspaceCommand>("create");
            la.AddCommand<GetWorkspaceCommand>("show");
            la.AddCommand<DeleteWorkspaceCommand>("delete");
            la.AddCommand<UpdateWorkspaceCommand>("update");
            la.AddCommand<ListWorkspacesCommand>("list");
        });
    }
}

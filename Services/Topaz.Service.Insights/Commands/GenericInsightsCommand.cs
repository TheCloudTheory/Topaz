using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.Insights.Commands;

public sealed class GenericInsightsCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("insights", insights =>
        {
            insights.AddBranch("component", component =>
            {
                component.AddCommand<CreateOrUpdateComponentCommand>("create");
                component.AddCommand<GetComponentCommand>("show");
                component.AddCommand<DeleteComponentCommand>("delete");
                component.AddCommand<ListComponentsCommand>("list");
                component.AddCommand<UpdateComponentCommand>("update");
            });
        });
    }
}

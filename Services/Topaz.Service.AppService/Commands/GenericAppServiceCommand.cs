using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.AppService.Commands;

namespace Topaz.Service.AppService.Commands;

public sealed class GenericAppServiceCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("appservice", appService =>
        {
            appService.AddBranch("plan", plan =>
            {
                plan.AddCommand<CreateAppServicePlanCommand>("create");
                plan.AddCommand<GetAppServicePlanCommand>("get");
                plan.AddCommand<DeleteAppServicePlanCommand>("delete");
                plan.AddCommand<ListAppServicePlansByResourceGroupCommand>("list");
                plan.AddCommand<RestartAppServicePlanSitesCommand>("restart-sites");
            });
        });
    }
}

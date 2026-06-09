using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.FinOps.Commands;

public sealed class GenericFinOpsCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("finops", finops =>
        {
            finops.AddCommand<EstimateCostsCommand>("estimate")
                .WithDescription("Estimate monthly costs for a subscription.");
        });
    }
}

using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands;

public sealed class GenericChaosCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("chaos", chaos =>
        {
            chaos.AddCommand<EnableChaosCommand>("enable");
            chaos.AddCommand<DisableChaosCommand>("disable");
            chaos.AddCommand<GetChaosStatusCommand>("status");
        });
    }
}

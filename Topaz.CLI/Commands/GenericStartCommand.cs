using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.CLI.Commands;

public sealed class GenericStartCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddCommand<StartCommand>("start");
    }
}
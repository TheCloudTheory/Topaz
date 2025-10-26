using Spectre.Console.Cli;
using Topaz.Service.Shared.Command;

namespace Topaz.CLI.Commands;

public sealed class GenericStartCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddCommand<StartCommand>("start");
    }
}
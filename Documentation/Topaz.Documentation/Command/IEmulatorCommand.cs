using Spectre.Console.Cli;

namespace Topaz.Documentation.Command;

public interface IEmulatorCommand
{
    void Configure(IConfigurator configurator);
}
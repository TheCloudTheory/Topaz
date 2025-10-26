using Spectre.Console.Cli;

namespace Topaz.Service.Shared.Command;

public interface IEmulatorCommand
{
    void Configure(IConfigurator configurator);
}
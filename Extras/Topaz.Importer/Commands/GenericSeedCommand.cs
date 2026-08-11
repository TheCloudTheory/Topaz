using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Importer.Commands;

public sealed class GenericSeedCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddCommand<SeedCommand>("seed")
            .WithDescription("Seed the Topaz host with data from a real cloud environment.");
    }
}
using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerRegistry.Commands;

public sealed class GenericContainerRegistryCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("acr", acr =>
        {
            acr.AddCommand<GenerateContainerRegistryCredentialsCommand>("generate-credentials");
        });
    }
}

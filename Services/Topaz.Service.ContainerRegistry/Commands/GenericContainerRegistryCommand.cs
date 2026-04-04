using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ContainerRegistry.Commands;

public sealed class GenericContainerRegistryCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("acr", acr =>
        {
            acr.AddCommand<CreateContainerRegistryCommand>("create");
            acr.AddCommand<DeleteContainerRegistryCommand>("delete");
            acr.AddCommand<ShowContainerRegistryCommand>("show");
            acr.AddCommand<ListContainerRegistriesCommand>("list");
            acr.AddCommand<UpdateContainerRegistryCommand>("update");
            acr.AddCommand<CheckContainerRegistryNameCommand>("check-name");
            acr.AddCommand<ListContainerRegistryCredentialsCommand>("list-credentials");
            acr.AddCommand<GenerateContainerRegistryCredentialsCommand>("generate-credentials");
        });
    }
}

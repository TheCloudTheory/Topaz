using Spectre.Console.Cli;
using Topaz.Documentation.Command;

namespace Topaz.Service.ManagedIdentity.Commands;

public sealed class GenericManagedIdentityCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("identity", identity => {
            identity.AddCommand<CreateManagedIdentityCommand>("create");
            identity.AddCommand<DeleteManagedIdentityCommand>("delete");
            identity.AddCommand<ShowManagedIdentityCommand>("show");
            identity.AddCommand<ListManagedIdentityCommand>("list");
            identity.AddCommand<UpdateManagedIdentityCommand>("update");
        });
    }
}

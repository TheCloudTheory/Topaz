using Spectre.Console.Cli;
using Topaz.Service.Shared.Command;

namespace Topaz.Service.KeyVault.Commands;

public sealed class GenericKeyVaultCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("keyvault", keyVault => {
            keyVault.AddCommand<CreateKeyVaultCommand>("create");
            keyVault.AddCommand<DeleteKeyVaultCommand>("delete");
            keyVault.AddCommand<CheckKeyVaultNameCommand>("check-name");
        });
    }
}
using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Commands.Secrets;

namespace Topaz.Service.KeyVault.Commands;

public sealed class GenericKeyVaultCommand : IEmulatorCommand
{
    public void Configure(IConfigurator configurator)
    {
        configurator.AddBranch("keyvault", keyVault => {
            keyVault.AddCommand<CreateKeyVaultCommand>("create");
            keyVault.AddCommand<DeleteKeyVaultCommand>("delete");
            keyVault.AddCommand<CheckKeyVaultNameCommand>("check-name");
            keyVault.AddBranch("secret", secret =>
            {
                secret.AddCommand<SetSecretCommand>("set");
                secret.AddCommand<GetSecretCommand>("get");
                secret.AddCommand<ListSecretsCommand>("list");
                secret.AddCommand<UpdateSecretCommand>("update");
                secret.AddCommand<DeleteSecretCommand>("delete");
            });
        });
    }
}
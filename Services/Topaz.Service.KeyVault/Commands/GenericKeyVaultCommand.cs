using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Commands.Keys;
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
                secret.AddCommand<BackupSecretCommand>("backup");
                secret.AddCommand<RestoreSecretCommand>("restore");
                secret.AddCommand<GetSecretCommand>("get");
                secret.AddCommand<GetDeletedSecretCommand>("get-deleted");
                secret.AddCommand<ListSecretsCommand>("list");
                secret.AddCommand<ListDeletedSecretsCommand>("list-deleted");
                secret.AddCommand<ListSecretVersionsCommand>("list-versions");
                secret.AddCommand<UpdateSecretCommand>("update");
                secret.AddCommand<DeleteSecretCommand>("delete");
                secret.AddCommand<RecoverDeletedSecretCommand>("recover");
                secret.AddCommand<PurgeDeletedSecretCommand>("purge");
            });
            keyVault.AddBranch("key", key =>
            {
                key.AddCommand<BackupKeyCommand>("backup");
                key.AddCommand<RestoreKeyCommand>("restore");
                key.AddCommand<CreateKeyCommand>("create");
                key.AddCommand<ImportKeyCommand>("import");
                key.AddCommand<GetKeyCommand>("get");
                key.AddCommand<GetDeletedKeyCommand>("get-deleted");
                key.AddCommand<ListKeysCommand>("list");
                key.AddCommand<ListKeyVersionsCommand>("list-versions");
                key.AddCommand<UpdateKeyCommand>("update");
                key.AddCommand<DeleteKeyCommand>("delete");
            });
        });
    }
}
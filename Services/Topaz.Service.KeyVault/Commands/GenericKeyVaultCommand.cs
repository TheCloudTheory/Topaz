using Spectre.Console.Cli;
using Topaz.Documentation.Command;
using Topaz.Service.KeyVault.Commands.Certificates;
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
            keyVault.AddBranch("certificate", certificate =>
            {
                certificate.AddCommand<CreateCertificateCommand>("create");
                certificate.AddCommand<ImportCertificateCommand>("import");
                certificate.AddCommand<GetCertificateCommand>("get");
                certificate.AddCommand<ListCertificatesCommand>("list");
                certificate.AddCommand<ListCertificateVersionsCommand>("list-versions");
                certificate.AddCommand<UpdateCertificateCommand>("update");
                certificate.AddCommand<DeleteCertificateCommand>("delete");
                certificate.AddCommand<BackupCertificateCommand>("backup");
                certificate.AddCommand<RestoreCertificateCommand>("restore");
                certificate.AddCommand<GetDeletedCertificateCommand>("get-deleted");
                certificate.AddCommand<ListDeletedCertificatesCommand>("list-deleted");
                certificate.AddCommand<RecoverDeletedCertificateCommand>("recover");
                certificate.AddCommand<PurgeDeletedCertificateCommand>("purge");
                certificate.AddCommand<GetCertificateOperationCommand>("get-operation");
                certificate.AddCommand<CancelCertificateOperationCommand>("cancel-operation");
                certificate.AddCommand<DeleteCertificateOperationCommand>("delete-operation");
            });
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
                key.AddCommand<GetKeyAttestationCommand>("get-attestation");
                key.AddCommand<GetDeletedKeyCommand>("get-deleted");
                key.AddCommand<ListKeysCommand>("list");
                key.AddCommand<ListKeyVersionsCommand>("list-versions");
                key.AddCommand<UpdateKeyCommand>("update");
                key.AddCommand<DeleteKeyCommand>("delete");
                key.AddCommand<RecoverDeletedKeyCommand>("recover");
                key.AddCommand<RotateKeyCommand>("rotate");
                key.AddCommand<GetRandomBytesCommand>("random-bytes");
                key.AddCommand<EncryptKeyCommand>("encrypt");
                key.AddCommand<DecryptKeyCommand>("decrypt");
                key.AddCommand<WrapKeyCommand>("wrap");
                key.AddCommand<UnwrapKeyCommand>("unwrap");
                key.AddCommand<ReleaseKeyCommand>("release");
                key.AddCommand<SignKeyCommand>("sign");
                key.AddCommand<VerifyKeyCommand>("verify");
                key.AddBranch("rotation-policy", rp =>
                {
                    rp.AddCommand<GetKeyRotationPolicyCommand>("show");
                    rp.AddCommand<UpdateKeyRotationPolicyCommand>("update");
                });
            });
        });
    }
}

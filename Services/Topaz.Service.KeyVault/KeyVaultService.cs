using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Endpoints;
using Topaz.Service.KeyVault.Endpoints.AccessPolicies;
using Topaz.Service.KeyVault.Endpoints.Keys;
using Topaz.Service.KeyVault.Endpoints.Secrets;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault;

public sealed class KeyVaultService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-key-vault");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "keyvault";
    public string Name => "Azure Key Vault";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new SetSecretEndpoint(eventPipeline, logger),
        new BackupSecretEndpoint(eventPipeline, logger),
        new RestoreSecretEndpoint(eventPipeline, logger),
        new GetSecretsEndpoint(eventPipeline, logger),
        new GetSecretVersionsEndpoint(eventPipeline, logger),
        new GetSecretEndpoint(eventPipeline, logger),
        new DeleteSecretEndpoint(eventPipeline, logger),
        new GetDeletedSecretsEndpoint(eventPipeline, logger),
        new GetDeletedSecretEndpoint(eventPipeline, logger),
        new RecoverDeletedSecretEndpoint(eventPipeline, logger),
        new PurgeDeletedSecretEndpoint(eventPipeline, logger),
        new UpdateSecretEndpoint(eventPipeline, logger),
        new BackupKeyEndpoint(eventPipeline, logger),
        new RestoreKeyEndpoint(eventPipeline, logger),
        new GetDeletedKeysEndpoint(eventPipeline, logger),
        new GetDeletedKeyEndpoint(eventPipeline, logger),
        new RecoverDeletedKeyEndpoint(eventPipeline, logger),
        new PurgeDeletedKeyEndpoint(eventPipeline, logger),
        new RotateKeyEndpoint(eventPipeline, logger),
        new GetKeyRotationPolicyEndpoint(eventPipeline, logger),
        new UpdateKeyRotationPolicyEndpoint(eventPipeline, logger),
        new CreateKeyEndpoint(eventPipeline, logger),
        new ImportKeyEndpoint(eventPipeline, logger),
        new GetKeysEndpoint(eventPipeline, logger),
        new GetKeyVersionsEndpoint(eventPipeline, logger),
        new GetKeyEndpoint(eventPipeline, logger),
        new UpdateKeyEndpoint(eventPipeline, logger),
        new DeleteKeyEndpoint(eventPipeline, logger),
        new GetRandomBytesEndpoint(eventPipeline, logger),
        new EncryptKeyEndpoint(eventPipeline, logger),
        new DecryptKeyEndpoint(eventPipeline, logger),
        new SignKeyEndpoint(eventPipeline, logger),
        new VerifyKeyEndpoint(eventPipeline, logger),
        new CreateOrUpdateKeyVaultEndpoint(eventPipeline, logger),
        new GetKeyVaultEndpoint(eventPipeline, logger),
        new UpdateKeyVaultEndpoint(eventPipeline, logger),
        new DeleteKeyVaultEndpoint(eventPipeline, logger),
        new ListKeyVaultsByResourceGroupEndpoint(eventPipeline, logger),
        new ListKeyVaultsBySubscriptionEndpoint(eventPipeline, logger),
        new ListKeyVaultSubscriptionResourcesEndpoint(eventPipeline, logger),
        new ListDeletedVaultsEndpoint(eventPipeline, logger),
        new ListDeletedManagedHsmsEndpoint(eventPipeline, logger),
        new GetDeletedVaultEndpoint(eventPipeline, logger),
        new CheckKeyVaultNameAvailabilityEndpoint(eventPipeline, logger),
        new CheckMhsmNameAvailabilityEndpoint(eventPipeline, logger),
        new PurgeDeletedVaultEndpoint(eventPipeline, logger),
        new RecoverDeletedVaultEndpoint(eventPipeline, logger),
        new UpdateAccessPolicyEndpoint(eventPipeline, logger)
    ];

    public void Bootstrap()
    {
    }
}

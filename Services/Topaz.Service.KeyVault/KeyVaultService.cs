using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Endpoints;
using Topaz.Service.KeyVault.Endpoints.AccessPolicies;
using Topaz.Service.KeyVault.Endpoints.Certificates;
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
        // Certificate contacts — must come before single-cert endpoints to prevent
        // "contacts" being matched as a certificate name.
        new SetCertificateContactsEndpoint(eventPipeline, logger),
        new GetCertificateContactsEndpoint(eventPipeline, logger),
        new DeleteCertificateContactsEndpoint(eventPipeline, logger),
        // Certificate issuers — list before single to avoid "/issuers" matched as issuer name.
        // Both must come before the generic /certificates/{name} routes.
        new GetCertificateIssuersEndpoint(eventPipeline, logger),
        new SetCertificateIssuerEndpoint(eventPipeline, logger),
        new GetCertificateIssuerEndpoint(eventPipeline, logger),
        new UpdateCertificateIssuerEndpoint(eventPipeline, logger),
        new DeleteCertificateIssuerEndpoint(eventPipeline, logger),
        // Certificates — list endpoint must come before the single-GET to prevent GET /certificates/{empty}
        // matching before GET /certificates (trailing-slash path has same segment count as /{certName}).
        // Operation endpoint before versioned GET to avoid /pending being matched as a version.
        new GetCertificatesEndpoint(eventPipeline, logger),
        new MergeCertificateEndpoint(eventPipeline, logger),
        new GetCertificateOperationEndpoint(eventPipeline, logger),
        new UpdateCertificateOperationEndpoint(eventPipeline, logger),
        new DeleteCertificateOperationEndpoint(eventPipeline, logger),
        new GetCertificateVersionsEndpoint(eventPipeline, logger),
        new GetCertificateEndpoint(eventPipeline, logger),
        new CreateCertificateEndpoint(eventPipeline, logger),
        new ImportCertificateEndpoint(eventPipeline, logger),
        new UpdateCertificateEndpoint(eventPipeline, logger),
        new DeleteCertificateEndpoint(eventPipeline, logger),
        new RestoreCertificateEndpoint(eventPipeline, logger),
        new BackupCertificateEndpoint(eventPipeline, logger),
        // Deleted certificates — list before single-GET to prevent routing collision
        new GetDeletedCertificatesEndpoint(eventPipeline, logger),
        new GetDeletedCertificateEndpoint(eventPipeline, logger),
        new RecoverDeletedCertificateEndpoint(eventPipeline, logger),
        new PurgeDeletedCertificateEndpoint(eventPipeline, logger),
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
        new GetKeyAttestationEndpoint(eventPipeline, logger),
        new UpdateKeyEndpoint(eventPipeline, logger),
        new DeleteKeyEndpoint(eventPipeline, logger),
        new GetRandomBytesEndpoint(eventPipeline, logger),
        new EncryptKeyEndpoint(eventPipeline, logger),
        new DecryptKeyEndpoint(eventPipeline, logger),
        new SignKeyEndpoint(eventPipeline, logger),
        new VerifyKeyEndpoint(eventPipeline, logger),
        new WrapKeyEndpoint(eventPipeline, logger),
        new UnwrapKeyEndpoint(eventPipeline, logger),
        new ReleaseKeyEndpoint(eventPipeline, logger),
        new CreateOrUpdateKeyVaultEndpoint(eventPipeline, logger),
        new GetKeyVaultEndpoint(eventPipeline, logger),
        new UpdateKeyVaultEndpoint(eventPipeline, logger),
        new DeleteKeyVaultEndpoint(eventPipeline, logger),
        new ListKeyVaultsByResourceGroupEndpoint(eventPipeline, logger),
        new ListKeyVaultsBySubscriptionEndpoint(eventPipeline, logger),
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

using Azure.ResourceManager.Resources;
using Microsoft.Graph.Models;
using Topaz.Portal.Models.KeyVaults;
using Topaz.Portal.Models.Rbac;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.ResourceManager;
using Topaz.Portal.Models.Subscriptions;
using Topaz.Portal.Models.Tenant;

namespace Topaz.Portal;

public interface ITopazClient
{
    // Subscriptions
    Task<ListSubscriptionsResponse> ListSubscriptions();

    Task<SubscriptionDto?> GetSubscription(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task UpdateSubscriptionDisplayName(
        Guid subscriptionId,
        string displayName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateSubscriptionTag(
        Guid subscriptionId,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteSubscriptionTag(
        Guid subscriptionId,
        string tagName,
        CancellationToken cancellationToken = default);

    Task CreateSubscription(
        Guid subscriptionId,
        string subscriptionName,
        CancellationToken cancellationToken = default);

    // Resource Groups
    Task<ListResourceGroupsResponse> ListResourceGroups();

    Task<ListResourceGroupsResponse> ListResourceGroups(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<ResourceGroupDto?> GetResourceGroup(
        Guid subscriptionId,
        string resourceGroupName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateResourceGroupTag(
        Guid subscriptionId,
        string resourceGroupName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteResourceGroupTag(
        Guid subscriptionId,
        string resourceGroupName,
        string tagName,
        CancellationToken cancellationToken = default);

    Task<ListDeploymentsResponse> ListDeployments(
        Guid subscriptionId,
        string resourceGroupName,
        CancellationToken cancellationToken = default);

    Task<ArmDeploymentResource> GetDeployment(
        Guid subscriptionId,
        string resourceGroupName,
        string deploymentName,
        CancellationToken cancellationToken = default);

    Task CreateResourceGroup(
        Guid subscriptionId,
        string resourceGroupName,
        string location,
        CancellationToken cancellationToken = default);

    // Key Vault
    Task<ListKeyVaultsResponse> ListKeyVaults();

    Task CreateKeyVault(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        string location,
        string skuName = "Standard",
        CancellationToken cancellationToken = default);

    Task<KeyVaultDto?> GetKeyVault(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateKeyVaultTag(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteKeyVaultTag(
        Guid subscriptionId,
        string resourceGroupName,
        string keyVaultName,
        string tagName,
        CancellationToken cancellationToken = default);

    // Key Vault Secrets
    Task<IReadOnlyList<KeyVaultSecretDto>> ListKeyVaultSecrets(
        string vaultUri,
        CancellationToken cancellationToken = default);

    Task SetKeyVaultSecret(
        string vaultUri,
        string name,
        string value,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task DeleteKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KeyVaultDeletedSecretDto>> ListDeletedKeyVaultSecrets(
        string vaultUri,
        CancellationToken cancellationToken = default);

    Task<string> BackupKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default);

    Task RestoreKeyVaultSecret(
        string vaultUri,
        string backupBlob,
        CancellationToken cancellationToken = default);

    Task RecoverDeletedKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default);

    Task PurgeDeletedKeyVaultSecret(
        string vaultUri,
        string name,
        CancellationToken cancellationToken = default);

    // Authorization
    Task<ListRoleDefinitionsResponse?> ListRoleDefinitions(
        Guid subscriptionId,
        string? roleNameFilter = null,
        int pageSize = 5,
        string? continuationToken = null,
        CancellationToken cancellationToken = default);

    Task<ListRoleAssignmentsResponse?> ListRoleAssignments(
        Guid subscriptionId,
        string? roleNameFilter = null,
        int pageSize = 5,
        string? continuationToken = null,
        CancellationToken cancellationToken = default);

    // Entra
    Task<IReadOnlyList<User>> ListUsers(
        int top = 50,
        CancellationToken cancellationToken = default);

    Task CreateUser(
        string displayName,
        string userPrincipalName,
        string? mail,
        string password,
        bool accountEnabled = true,
        CancellationToken cancellationToken = default);

    Task<TenantInformationResponse> GetDirectoryInfo();
}

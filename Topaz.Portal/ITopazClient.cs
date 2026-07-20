using Azure.ResourceManager.Resources;
using Microsoft.Graph.Models;
using Topaz.Portal.Models;
using Topaz.Portal.Models.EventHubs;
using Topaz.Portal.Models.KeyVaults;
using Topaz.Portal.Models.ManagedIdentities;
using Topaz.Portal.Models.ManagementGroups;
using Topaz.Portal.Models.Rbac;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.ResourceManager;
using Topaz.Portal.Models.ServiceBus;
using Topaz.Portal.Models.CosmosDb;
using Topaz.Portal.Models.Sql;
using Topaz.Portal.Models.Storage;
using Topaz.Portal.Models.Subscriptions;
using Topaz.Portal.Models.Tenant;
using Topaz.Portal.Models.FinOps;
using Topaz.Portal.Models.VirtualMachines;
using Topaz.Portal.Models.VirtualNetworks;
using Topaz.Portal.Models.PublicIps;
using Topaz.Portal.Models.ContainerRegistry;
using Topaz.Portal.Models.Insights;

namespace Topaz.Portal;

public interface ITopazClient
{
    // FinOps
    Task<EstimatedCostsResponse?> GetEstimatedCosts(
        Guid subscriptionId,
        string currency = "USD",
        CancellationToken cancellationToken = default);

    // Management Groups
    Task<GetManagementGroupEntitiesResponse> GetManagementGroupEntities(
        CancellationToken cancellationToken = default);

    Task CreateManagementGroup(
        string groupId,
        string displayName,
        string? parentGroupId = null,
        CancellationToken cancellationToken = default);

    Task AssociateSubscriptionWithManagementGroup(
        string groupId,
        string subscriptionId,
        CancellationToken cancellationToken = default);

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

    // Storage Accounts
    Task<ListStorageAccountsResponse> ListStorageAccounts();

    Task<StorageAccountDto?> GetStorageAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        CancellationToken cancellationToken = default);

    Task CreateStorageAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        string location,
        string skuName = "Standard_LRS",
        string kind = "StorageV2",
        CancellationToken cancellationToken = default);

    Task DeleteStorageAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateStorageAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteStorageAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string storageAccountName,
        string tagName,
        CancellationToken cancellationToken = default);

    // Azure SQL
    Task<ListSqlServersResponse> ListSqlServers(
        CancellationToken cancellationToken = default);

    Task<SqlServerDto?> GetSqlServer(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken = default);

    Task CreateSqlServer(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string location,
        string administratorLogin,
        string administratorLoginPassword,
        string version = "12.0",
        CancellationToken cancellationToken = default);

    Task DeleteSqlServer(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateSqlServerTag(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteSqlServerTag(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string tagName,
        CancellationToken cancellationToken = default);

    Task<ListSqlDatabasesResponse> ListSqlDatabases(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        CancellationToken cancellationToken = default);

    Task<SqlDatabaseDto?> GetSqlDatabase(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default);

    Task CreateSqlDatabase(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default);

    Task DeleteSqlDatabase(
        Guid subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default);

    // Cosmos DB
    Task<ListCosmosDbAccountsResponse> ListCosmosDbAccounts(
        CancellationToken cancellationToken = default);

    Task<CosmosDbAccountDto?> GetCosmosDbAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        CancellationToken cancellationToken = default);

    Task CreateCosmosDbAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        string location,
        CancellationToken cancellationToken = default);

    Task DeleteCosmosDbAccount(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateCosmosDbAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteCosmosDbAccountTag(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        string tagName,
        CancellationToken cancellationToken = default);

    Task<CosmosDbAccountKeysDto> GetCosmosDbAccountKeys(
        Guid subscriptionId,
        string resourceGroupName,
        string accountName,
        CancellationToken cancellationToken = default);

    // Managed Identities
    Task<ListManagedIdentitiesResponse> ListManagedIdentities(
        CancellationToken cancellationToken = default);

    Task<ManagedIdentityDto?> GetManagedIdentity(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        CancellationToken cancellationToken = default);

    Task CreateManagedIdentity(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string location,
        CancellationToken cancellationToken = default);

    Task DeleteManagedIdentity(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateManagedIdentityTag(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteManagedIdentityTag(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string tagName,
        CancellationToken cancellationToken = default);

    Task<ListFederatedCredentialsResponse> ListFederatedCredentials(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        CancellationToken cancellationToken = default);

    Task CreateFederatedCredential(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string credentialName,
        string issuer,
        string subject,
        IList<string> audiences,
        CancellationToken cancellationToken = default);

    Task DeleteFederatedCredential(
        Guid subscriptionId,
        string resourceGroupName,
        string identityName,
        string credentialName,
        CancellationToken cancellationToken = default);

    // Event Hubs
    Task<ListEventHubNamespacesResponse> ListEventHubNamespaces(
        CancellationToken cancellationToken = default);

    Task<EventHubNamespaceDto?> GetEventHubNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default);

    Task CreateEventHubNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string location,
        CancellationToken cancellationToken = default);

    Task DeleteEventHubNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateEventHubNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteEventHubNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        CancellationToken cancellationToken = default);

    Task<ListEventHubsResponse> ListEventHubs(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default);

    Task CreateEventHub(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string eventHubName,
        int partitionCount = 4,
        int messageRetentionInDays = 1,
        CancellationToken cancellationToken = default);

    Task DeleteEventHub(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string eventHubName,
        CancellationToken cancellationToken = default);

    // Service Bus
    Task<ListServiceBusNamespacesResponse> ListServiceBusNamespaces(
        CancellationToken cancellationToken = default);

    Task<ServiceBusNamespaceDto?> GetServiceBusNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default);

    Task CreateServiceBusNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string location,
        CancellationToken cancellationToken = default);

    Task DeleteServiceBusNamespace(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateServiceBusNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteServiceBusNamespaceTag(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string tagName,
        CancellationToken cancellationToken = default);

    Task<ListServiceBusQueuesResponse> ListServiceBusQueues(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default);

    Task CreateServiceBusQueue(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string queueName,
        CancellationToken cancellationToken = default);

    Task DeleteServiceBusQueue(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string queueName,
        CancellationToken cancellationToken = default);

    Task<ListServiceBusTopicsResponse> ListServiceBusTopics(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        CancellationToken cancellationToken = default);

    Task CreateServiceBusTopic(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string topicName,
        CancellationToken cancellationToken = default);

    Task DeleteServiceBusTopic(
        Guid subscriptionId,
        string resourceGroupName,
        string namespaceName,
        string topicName,
        CancellationToken cancellationToken = default);

    // Container Registry
    Task<ListContainerRegistriesResponse> ListContainerRegistries();

    Task CreateContainerRegistry(
        Guid subscriptionId,
        string resourceGroupName,
        string registryName,
        string location,
        string skuName = "Basic",
        CancellationToken cancellationToken = default);

    Task DeleteContainerRegistry(
        Guid subscriptionId,
        string resourceGroupName,
        string registryName,
        CancellationToken cancellationToken = default);

    Task<ContainerRegistryDto?> GetContainerRegistry(
        Guid subscriptionId,
        string resourceGroupName,
        string registryName,
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

    // Host
    Task<HostInfoDto?> GetHostInfoAsync(CancellationToken cancellationToken = default);

    // Virtual Machines
    Task<ListVirtualMachinesResponse> ListVirtualMachines(CancellationToken cancellationToken = default);

    Task<VirtualMachineDto?> GetVirtualMachine(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        CancellationToken cancellationToken = default);

    Task CreateVirtualMachine(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        string location,
        string vmSize = "Standard_B2s",
        CancellationToken cancellationToken = default);

    Task DeleteVirtualMachine(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateVirtualMachineTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteVirtualMachineTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vmName,
        string tagName,
        CancellationToken cancellationToken = default);

    // Virtual Networks
    Task<ListVirtualNetworksResponse> ListVirtualNetworks(CancellationToken cancellationToken = default);

    Task<VirtualNetworkDto?> GetVirtualNetwork(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        CancellationToken cancellationToken = default);

    Task CreateVirtualNetwork(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        string location,
        string addressPrefix = "10.0.0.0/16",
        CancellationToken cancellationToken = default);

    Task DeleteVirtualNetwork(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateVirtualNetworkTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteVirtualNetworkTag(
        Guid subscriptionId,
        string resourceGroupName,
        string vnetName,
        string tagName,
        CancellationToken cancellationToken = default);

    // Public IP Addresses
    Task<ListPublicIpAddressesResponse> ListPublicIpAddresses(CancellationToken cancellationToken = default);

    Task<PublicIpAddressDto?> GetPublicIpAddress(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        CancellationToken cancellationToken = default);

    Task CreatePublicIpAddress(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        string location,
        string allocationMethod = "Static",
        string ipVersion = "IPv4",
        string sku = "Standard",
        CancellationToken cancellationToken = default);

    Task DeletePublicIpAddress(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdatePublicIpAddressTag(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeletePublicIpAddressTag(
        Guid subscriptionId,
        string resourceGroupName,
        string pipName,
        string tagName,
        CancellationToken cancellationToken = default);

    // Application Insights
    Task<ListApplicationInsightsResponse> ListApplicationInsights();

    Task<ApplicationInsightsDto?> GetApplicationInsights(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        CancellationToken cancellationToken = default);

    Task CreateApplicationInsights(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        string location,
        string kind = "web",
        string applicationType = "web",
        CancellationToken cancellationToken = default);

    Task DeleteApplicationInsights(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        CancellationToken cancellationToken = default);

    Task CreateOrUpdateApplicationInsightsTag(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        string tagName,
        string tagValue,
        CancellationToken cancellationToken = default);

    Task DeleteApplicationInsightsTag(
        Guid subscriptionId,
        string resourceGroupName,
        string componentName,
        string tagName,
        CancellationToken cancellationToken = default);

    Task<ApplicationInsightsQueryResponse> QueryApplicationInsights(
        string instrumentationKey,
        string query,
        CancellationToken cancellationToken = default);
}

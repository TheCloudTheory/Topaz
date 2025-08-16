using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.ServiceBus;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Security.KeyVault.Secrets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Topaz.AspNetCore.Extensions;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Tests.Isolated;

public class AspNetCoreExtensionTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "testsatopaz";
    private const string KeyVaultName = "kvtesttopaz";
    private const string ServiceBusNamespaceName = "sb-test";
    private const string ServiceBusQueueName = "sb-queue-test";

    private IContainer? _container;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _container = new ContainerBuilder()
            .WithImage("thecloudtheory/topaz-cli:v1.0.155-alpha")
            .WithPortBinding(8890)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8897)
            .WithPortBinding(8891)
            .Build();

        await _container.StartAsync()
            .ConfigureAwait(false);
        
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _container!.DisposeAsync();
    }

    [Test]
    public async Task WhenStorageAccountConnectionStringIsAddedAsSecret_ItMustBeAvailable()
    {
        // Arrange
        const string secretName = "connectionString-storageAccount";
        var subscriptionId = Guid.NewGuid();
        var builder = new ConfigurationBuilder();
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(KeyVaultName), credential: credentials, new SecretClientOptions
        {
            DisableChallengeResourceVerification = true
        });
        
        // Act
        await builder.AddTopaz(subscriptionId)
            .AddSubscription(subscriptionId, SubscriptionName)
            .AddResourceGroup(subscriptionId, ResourceGroupName, AzureLocation.WestEurope)
            .AddStorageAccount(ResourceGroupIdentifier.From(ResourceGroupName), StorageAccountName,
                new StorageAccountCreateOrUpdateContent(new StorageSku(StorageSkuName.StandardLrs),
                    StorageKind.StorageV2, AzureLocation.WestEurope))
            .AddKeyVault(ResourceGroupIdentifier.From(ResourceGroupName), KeyVaultName,
                new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                    new KeyVaultProperties(Guid.Empty,
                        new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))))
            .AddStorageAccountConnectionStringAsSecret(ResourceGroupIdentifier.From(ResourceGroupName), StorageAccountName, KeyVaultName,
                secretName);
        
        var secret = await client.GetSecretAsync(secretName);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(StorageAccountName);
        var key = storageAccount.Value.GetKeys().ToArray()[0];

        // Assert
        Assert.That(secret, Is.Not.Null);
        Assert.That(secret.Value, Is.Not.Null);
        Assert.That(secret.Value.Value, Does.Contain(key.Value));
    }

    [Test]
    public async Task WhenServiceBusNamespaceIsCreated_ItMustBeAvailable()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var credentials = new AzureLocalCredential();
        var subscriptionId = Guid.NewGuid();
        
        // Act
        await builder.AddTopaz(subscriptionId)
            .AddSubscription(subscriptionId, SubscriptionName)
            .AddResourceGroup(subscriptionId, ResourceGroupName, AzureLocation.WestEurope)
            .AddServiceBusNamespace(ResourceGroupIdentifier.From(ResourceGroupName), ServiceBusNamespaceIdentifier.From(ServiceBusNamespaceName),
                new ServiceBusNamespaceData(AzureLocation.WestEurope));
        
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(ServiceBusNamespaceName);

        // Assert
        Assert.That(@namespace, Is.Not.Null);
        Assert.That(@namespace.Value, Is.Not.Null);
        Assert.That(@namespace.Value.Data.Name, Is.EqualTo(ServiceBusNamespaceName));
    }
    
    /*[Test]
    public async Task WhenServiceBusQueueIsCreated_ItMustBeAvailable()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var credentials = new AzureLocalCredential();
        
        // Act
        await builder.AddTopaz(SubscriptionId)
            .AddSubscription(SubscriptionId, SubscriptionName)
            .AddResourceGroup(SubscriptionId, ResourceGroupName, AzureLocation.WestEurope)
            .AddServiceBusNamespace(ResourceGroupIdentifier.From(ResourceGroupName), ServiceBusNamespaceIdentifier.From(ServiceBusNamespaceName),
                new ServiceBusNamespaceData(AzureLocation.WestEurope))
            .AddServiceBusQueue(ResourceGroupIdentifier.From(ResourceGroupName), ServiceBusNamespaceIdentifier.From(ServiceBusNamespaceName), ServiceBusQueueName, new ServiceBusQueueData());
        
        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var @namespace = await resourceGroup.Value.GetServiceBusNamespaceAsync(ServiceBusNamespaceName);
        var queue = await @namespace.Value.GetServiceBusQueueAsync(ServiceBusQueueName);

        // Assert
        Assert.That(queue, Is.Not.Null);
        Assert.That(queue.Value, Is.Not.Null);
        Assert.That(queue.Value.Data.Name, Is.EqualTo(ServiceBusQueueName));
    }*/
}
using System.Text.Json;
using System.Xml.Serialization;
using Azure.Core;
using Azure.Data.Tables.Models;
using Azure.ResourceManager.Storage.Models;
using Topaz.Dns;
using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Service.Storage.Services;
using Topaz.Shared;
using TableAnalyticsLoggingSettings = Topaz.Service.Storage.Models.TableAnalyticsLoggingSettings;
using TableMetrics = Topaz.Service.Storage.Models.TableMetrics;
using TableServiceProperties = Topaz.Service.Storage.Models.TableServiceProperties;

namespace Topaz.Service.Storage;

internal sealed class AzureStorageControlPlane(ResourceProvider provider, ITopazLogger logger) : IControlPlane
{
    public static AzureStorageControlPlane New(ITopazLogger logger)
    {
        return new AzureStorageControlPlane(new ResourceProvider(logger), logger);
    }
    
    public (OperationResult result, StorageAccountResource? resource) Get(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var storageAccount = provider.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (string.IsNullOrEmpty(storageAccount))
        {
            return (OperationResult.NotFound, null);
        }

        var resource = JsonSerializer.Deserialize<StorageAccountResource>(storageAccount, GlobalSettings.JsonOptions);

        return resource == null ? (OperationResult.Failed, null) : (OperationResult.Success, resource);
    }

    public (OperationResult result, StorageAccountResource? resource) Create(string storageAccountName,
        ResourceGroupIdentifier resourceGroupIdentifier, AzureLocation location,
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var storageAccount = provider.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (!string.IsNullOrEmpty(storageAccount))
        {
            logger.LogError($"Storage account '{storageAccountName}' already exists.");
            return (OperationResult.Failed, null);
        }

        var sku = new ResourceSku
        {
            Name = StorageSkuName.StandardLrs.ToString()
        };
        var properties = new StorageAccountResourceProperties
        {
            PrimaryEndpoints = BuildPrimaryEndpoints(storageAccountName)
        };
        var resource = new StorageAccountResource(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            location, sku, StorageKind.StorageV2.ToString(), properties);

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, resource);

        InitializeServicePropertiesFiles(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return (OperationResult.Created, resource);
    }

    public (OperationResult result, CheckStorageAccountNameAvailabilityResponse response) CheckNameAvailability(
        SubscriptionIdentifier subscriptionIdentifier, string storageAccountName, string? resourceType)
    {
        if (!IsStorageAccountNameValid(storageAccountName))
        {
            return (OperationResult.Success, new CheckStorageAccountNameAvailabilityResponse
            {
                NameAvailable = false,
                Reason = CheckStorageAccountNameAvailabilityResponse.NameUnavailableReason.AccountNameInvalid,
                Message = $"The storage account name '{storageAccountName}' is invalid. A storage account name must be 3-24 characters long and use lowercase letters and numbers only."
            });
        }

        var dnsEntry = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, storageAccountName);
        if (dnsEntry == null)
        {
            return (OperationResult.Success, new CheckStorageAccountNameAvailabilityResponse
            {
                NameAvailable = true
            });
        }

        var existingStorageAccount = provider.GetAs<StorageAccountResource>(subscriptionIdentifier,
            ResourceGroupIdentifier.From(dnsEntry.Value.resourceGroup), storageAccountName);
        if (existingStorageAccount == null)
        {
            return (OperationResult.Success, new CheckStorageAccountNameAvailabilityResponse
            {
                NameAvailable = true
            });
        }

        if (!string.IsNullOrWhiteSpace(resourceType) &&
            !string.Equals(existingStorageAccount.Type, resourceType, StringComparison.OrdinalIgnoreCase))
        {
            return (OperationResult.Success, new CheckStorageAccountNameAvailabilityResponse
            {
                NameAvailable = true
            });
        }

        return (OperationResult.Success, new CheckStorageAccountNameAvailabilityResponse
        {
            NameAvailable = false,
            Reason = CheckStorageAccountNameAvailabilityResponse.NameUnavailableReason.AlreadyExists,
            Message = $"The storage account name '{storageAccountName}' is already in use."
        });
    }

    private void InitializeServicePropertiesFiles(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        const string propertiesFile = $"properties.xml";
        var propertiesFilePath =
            Path.Combine(
                provider.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName),
                propertiesFile);

        logger.LogDebug(nameof(AzureStorageControlPlane), nameof(InitializeServicePropertiesFiles),
            "Attempting to create {0} file.", propertiesFilePath);

        if (!File.Exists(propertiesFilePath))
        {
            var logging = new TableAnalyticsLoggingSettings("1.0", true, true, true, new TableRetentionPolicy(true)
            {
                Days = 7
            });
            var metrics = new TableMetrics(true);
            var properties = new TableServiceProperties(logging, metrics, metrics, new List<TableCorsRule>()
            {
                new("http://localhost", "*", "*", "*", 500)
            });

            using var sw = new StreamWriter(propertiesFilePath);
            var serializer = new XmlSerializer(typeof(TableServiceProperties));
            serializer.Serialize(sw, properties);
        }
        else
        {
            logger.LogDebug(nameof(AzureStorageControlPlane), nameof(InitializeServicePropertiesFiles),
                "Attempting to create {0} file - skipped.", propertiesFilePath);
        }
    }

    internal void Delete(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName)
    {
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
    }

    public string GetServiceInstancePath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        return provider.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
    }

    public (OperationResult result, StorageAccountResource resource) CreateOrUpdate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        CreateOrUpdateStorageAccountRequest request)
    {
        var existingAccount = provider.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var properties = request.Properties! with { PrimaryEndpoints = BuildPrimaryEndpoints(storageAccountName) };
        var resource = new StorageAccountResource(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            request.Location!,
            request.Sku!, request.Kind!, properties);

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, resource,
            existingAccount == null);

        InitializeServicePropertiesFiles(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);

        return (string.IsNullOrWhiteSpace(existingAccount) ? OperationResult.Created : OperationResult.Updated,
            resource);
    }

    private static bool IsStorageAccountNameValid(string storageAccountName)
    {
        return !string.IsNullOrWhiteSpace(storageAccountName)
               && storageAccountName.Length is >= 3 and <= 24
               && storageAccountName.All(character => char.IsLower(character) || char.IsDigit(character));
    }

    private static StorageAccountPrimaryEndpoints BuildPrimaryEndpoints(string accountName) => new()
    {
        Blob = $"https://{accountName}.blob.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/",
        Queue = $"https://{accountName}.queue.storage.topaz.local.dev:{GlobalSettings.DefaultQueueStoragePort}/",
        Table = $"https://{accountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}/",
        File = $"https://{accountName}.file.storage.topaz.local.dev:{GlobalSettings.DefaultFileStoragePort}/",
        Web = $"https://{accountName}.web.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/",
        Dfs = $"https://{accountName}.dfs.storage.topaz.local.dev:{GlobalSettings.DefaultBlobStoragePort}/",
    };

    // LocalDirectoryPath has 5 segments; add 3 for .topaz prefix, account-name dir, and metadata.json
    private static readonly uint StorageAccountFileSegmentCount =
        (uint)(AzureStorageService.LocalDirectoryPath.Split("/").Length + 3);

    public ControlPlaneOperationResult<StorageAccountResource[]> List(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources =
            provider.ListAs<StorageAccountResource>(subscriptionIdentifier, resourceGroupIdentifier, null,
                StorageAccountFileSegmentCount);
        var filteredResources = resources.Where(resource =>
            resource.IsInSubscription(subscriptionIdentifier) && resource.IsInResourceGroup(resourceGroupIdentifier));

        return new ControlPlaneOperationResult<StorageAccountResource[]>(OperationResult.Success,
            filteredResources.ToArray(), null, null);
    }

    public ControlPlaneOperationResult<StorageAccountResource[]> ListBySubscription(
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var resources = provider.ListAs<StorageAccountResource>(subscriptionIdentifier, null,
                lookForNoOfSegments: StorageAccountFileSegmentCount)
            .Where(r => r.IsInSubscription(subscriptionIdentifier))
            .ToArray();

        return new ControlPlaneOperationResult<StorageAccountResource[]>(OperationResult.Success, resources, null, null);
    }

    public OperationResult Deploy(GenericResource resource)
    {
        var storageAccount = resource.As<StorageAccountResource, StorageAccountResourceProperties>();
        if (storageAccount == null)
        {
            logger.LogError($"Couldn't parse generic resource `{resource.Id}` as a Managed Identity instance.");
            return OperationResult.Failed;
        }

        var result = CreateOrUpdate(storageAccount.GetSubscription(), storageAccount.GetResourceGroup(), storageAccount.Name,
            new CreateOrUpdateStorageAccountRequest
            {
                Location = storageAccount.Location,
                Tags = storageAccount.Tags,
                Properties = storageAccount.Properties
            });

        return result.result;
    }
}
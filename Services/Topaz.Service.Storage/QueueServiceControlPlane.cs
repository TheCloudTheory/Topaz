using System.Text.Json;
using System.Xml.Linq;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class QueueServiceControlPlane(QueueResourceProvider provider, ITopazLogger logger)
{
    public static QueueServiceControlPlane New(ITopazLogger logger) => new(new QueueResourceProvider(logger), logger);
    public ControlPlaneOperationResult<QueueProperties[]> ListQueues(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var queues = provider.ListAs<Queue>(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, 10);
        var result = queues.Select(queue => new QueueProperties
        {
            Name = queue.Name,
            CreatedTime = DateTimeOffset.UtcNow,
            UpdatedTime = DateTimeOffset.UtcNow,
            ApproximateMessageCount = 0,
            Metadata = new Dictionary<string, string>()
        }).ToArray()!;

        return new ControlPlaneOperationResult<QueueProperties[]>(OperationResult.Success, result, null, null);
    }

    public ControlPlaneOperationResult<Queue> CreateQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        var now = DateTimeOffset.UtcNow;
        var queue = new Queue
        {
            Name = queueName,
            Properties = new QueueProperties
            {
                Name = queueName,
                CreatedTime = now,
                UpdatedTime = now,
                ApproximateMessageCount = 0,
                Metadata = new Dictionary<string, string>()
            }
        };

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName, queue);

        // Persist properties
        var propertiesPath = provider.GetQueuePropertiesFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);
        File.WriteAllText(propertiesPath, JsonSerializer.Serialize(queue.Properties, GlobalSettings.JsonOptions));

        return new ControlPlaneOperationResult<Queue>(OperationResult.Created, queue, null, null);
    }

    public ControlPlaneOperationResult DeleteQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public bool QueueExists(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        return provider.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
    }

    public (bool exists, string aclFilePath) GetQueueAclState(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        var exists = provider.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
        var filePath = provider.GetQueueAclFilePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName);
        return (exists, filePath);
    }

    public ControlPlaneOperationResult SetQueueMetadata(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName,
        Dictionary<string, string> metadata)
    {
        if (!provider.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new ControlPlaneOperationResult(OperationResult.NotFound, "Queue not found.", "QueueNotFound");
        }

        var propertiesPath = provider.GetQueuePropertiesFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        QueueProperties properties;
        if (File.Exists(propertiesPath))
        {
            var content = File.ReadAllText(propertiesPath);
            properties = JsonSerializer.Deserialize<QueueProperties>(content, GlobalSettings.JsonOptions)
                ?? new QueueProperties();
        }
        else
        {
            properties = new QueueProperties { Name = queueName };
        }

        properties.Metadata = metadata;
        properties.UpdatedTime = DateTimeOffset.UtcNow;

        File.WriteAllText(propertiesPath, JsonSerializer.Serialize(properties, GlobalSettings.JsonOptions));

        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public ControlPlaneOperationResult<string> GetQueueServicePropertiesXml(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName)
    {
        var storageControlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var path = storageControlPlane.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName);
        var propertiesFilePath = Path.Combine(path, "queue-service-properties.xml");

        if (!File.Exists(propertiesFilePath))
            return new ControlPlaneOperationResult<string>(OperationResult.Success, DefaultQueueServicePropertiesXml,
                null, null);

        return new ControlPlaneOperationResult<string>(OperationResult.Success,
            File.ReadAllText(propertiesFilePath), null, null);
    }

    public ControlPlaneOperationResult SetQueueServiceProperties(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, Stream input)
    {
        var storageControlPlane = new AzureStorageControlPlane(new StorageResourceProvider(logger), logger);
        var path = storageControlPlane.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName);
        var propertiesFilePath = Path.Combine(path, "queue-service-properties.xml");

        var document = XDocument.Load(input, LoadOptions.PreserveWhitespace);

        if (document.Root?.Element("Cors") == null)
            document.Root?.Add(new XElement("Cors"));

        document.Save(propertiesFilePath);
        return new ControlPlaneOperationResult(OperationResult.Success);
    }

    public static string GetQueueServiceStatsXml()
    {
        var lastSyncTime = DateTimeOffset.UtcNow.ToString("R");
        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <StorageServiceStats>
                  <GeoReplication>
                    <Status>live</Status>
                    <LastSyncTime>{lastSyncTime}</LastSyncTime>
                  </GeoReplication>
                </StorageServiceStats>
                """;
    }

    private const string DefaultQueueServicePropertiesXml =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<StorageServiceProperties>" +
        "<Logging><Version>1.0</Version><Delete>false</Delete><Read>false</Read><Write>false</Write>" +
        "<RetentionPolicy><Enabled>false</Enabled></RetentionPolicy></Logging>" +
        "<HourMetrics><Version>1.0</Version><Enabled>false</Enabled>" +
        "<RetentionPolicy><Enabled>false</Enabled></RetentionPolicy></HourMetrics>" +
        "<MinuteMetrics><Version>1.0</Version><Enabled>false</Enabled>" +
        "<RetentionPolicy><Enabled>false</Enabled></RetentionPolicy></MinuteMetrics>" +
        "<Cors /></StorageServiceProperties>";

    public ControlPlaneOperationResult<QueueProperties> GetQueueProperties(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        if (!provider.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, queueName))
        {
            return new ControlPlaneOperationResult<QueueProperties>(OperationResult.NotFound, null,
                "Queue not found.", "QueueNotFound");
        }

        var propertiesPath = provider.GetQueuePropertiesFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        if (!File.Exists(propertiesPath))
        {
            return new ControlPlaneOperationResult<QueueProperties>(OperationResult.NotFound, null,
                "Queue properties file not found.", "QueuePropertiesNotFound");
        }

        var content = File.ReadAllText(propertiesPath);
        var properties = JsonSerializer.Deserialize<QueueProperties>(content, GlobalSettings.JsonOptions);

        return new ControlPlaneOperationResult<QueueProperties>(OperationResult.Success, properties, null, null);
    }
}

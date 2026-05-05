using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Topaz.MCP.Prompts;

[McpServerPromptType]
[Description("Prompts for provisioning common Azure application stacks in a running Topaz emulator.")]
[UsedImplicitly]
public sealed class ScenarioPrompts
{
    [McpServerPrompt(Name = "setup-web-app-backend")]
    [Description("Provisions a typical web-app backend stack: a Storage Account with a Blob container plus a Key Vault seeded with a database connection string. Returns all endpoints and the vault URI.")]
    [UsedImplicitly]
    public static IList<PromptMessage> SetupWebAppBackend(
        [Description("Subscription ID containing the resource group.")]
        string subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Storage account name (lowercase, 3–24 chars).")]
        string storageAccountName,
        [Description("Blob container name for static assets or uploads.")]
        string containerName,
        [Description("Key Vault name.")]
        string keyVaultName,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId,
        [Description("Optional name of the initial Key Vault secret (e.g. 'db-connection-string').")]
        string? secretName = null,
        [Description("Optional value of the initial Key Vault secret. Required when secretName is provided.")]
        string? secretValue = null)
    {
        var kvSecretLine = secretName is not null && secretValue is not null
            ? $", secretName=\"{secretName}\", secretValue=\"{secretValue}\""
            : "";

        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Provision a web-app backend stack in Topaz by performing these steps in order:

                            1. Call CreateStorageAccount with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", location="{location}", objectId="{objectId}".
                            2. Call CreateBlobContainer with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", containerName="{containerName}", objectId="{objectId}".
                            3. Call CreateKeyVault with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", keyVaultName="{keyVaultName}", location="{location}", objectId="{objectId}"{kvSecretLine}.
                            4. Call GetConnectionStrings with subscriptionId="{subscriptionId}", objectId="{objectId}" to retrieve the final endpoint inventory.

                            After all steps complete, provide:
                            - Storage account connection string and Blob service URI.
                            - The blob container name.
                            - Key Vault URI{(secretName is not null ? $" and confirmation that the secret '{secretName}' was seeded" : "")}.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "setup-functions-local-dev")]
    [Description("Provisions the Azure Functions local-dev stack: a Storage Account (runtime requirement), a Service Bus namespace with a trigger queue, and a Key Vault seeded with the AzureWebJobsStorage connection string.")]
    [UsedImplicitly]
    public static IList<PromptMessage> SetupFunctionsLocalDev(
        [Description("Subscription ID containing the resource group.")]
        string subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Storage account name used by the Functions runtime (lowercase, 3–24 chars).")]
        string storageAccountName,
        [Description("Service Bus namespace name.")]
        string serviceBusNamespaceName,
        [Description("Service Bus queue name used as a function trigger.")]
        string triggerQueueName,
        [Description("Key Vault name where AzureWebJobsStorage will be stored.")]
        string keyVaultName,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId)
    {
        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Provision an Azure Functions local development stack in Topaz by performing these steps in order:

                            1. Call CreateStorageAccount with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", location="{location}", objectId="{objectId}". Note the returned connection string — it will be used as the AzureWebJobsStorage secret.
                            2. Call CreateServiceBusNamespace with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{serviceBusNamespaceName}", location="{location}", objectId="{objectId}".
                            3. Call CreateServiceBusQueue with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{serviceBusNamespaceName}", queueName="{triggerQueueName}", objectId="{objectId}".
                            4. Call CreateKeyVault with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", keyVaultName="{keyVaultName}", location="{location}", objectId="{objectId}", secretName="AzureWebJobsStorage", secretValue=<connection string from step 1>.

                            After all steps complete, provide:
                            - The AzureWebJobsStorage connection string for local.settings.json.
                            - The Service Bus connection string for the trigger binding.
                            - The Key Vault URI.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "setup-event-driven-microservice")]
    [Description("Provisions a Service Bus namespace with a command queue, an event topic with a subscription, and a Key Vault for credentials. Models the command/event split common in event-driven microservices.")]
    [UsedImplicitly]
    public static IList<PromptMessage> SetupEventDrivenMicroservice(
        [Description("Subscription ID containing the resource group.")]
        string subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Service Bus namespace name.")]
        string namespaceName,
        [Description("Queue name for incoming commands.")]
        string commandQueueName,
        [Description("Topic name for outgoing domain events.")]
        string eventTopicName,
        [Description("Subscription name on the event topic.")]
        string subscriptionName,
        [Description("Key Vault name for storing the Service Bus connection string.")]
        string keyVaultName,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId)
    {
        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Provision an event-driven microservice stack in Topaz by performing these steps in order:

                            1. Call CreateServiceBusNamespace with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{namespaceName}", location="{location}", objectId="{objectId}". Note the returned connection string.
                            2. Call CreateServiceBusQueue with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{namespaceName}", queueName="{commandQueueName}", objectId="{objectId}".
                            3. Call CreateServiceBusTopic with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{namespaceName}", topicName="{eventTopicName}", objectId="{objectId}".
                            4. Call CreateServiceBusSubscription with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{namespaceName}", topicName="{eventTopicName}", subscriptionName="{subscriptionName}", objectId="{objectId}".
                            5. Call CreateKeyVault with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", keyVaultName="{keyVaultName}", location="{location}", objectId="{objectId}", secretName="ServiceBusConnectionString", secretValue=<connection string from step 1>.

                            After all steps complete, provide:
                            - Command queue connection string.
                            - Event topic and subscription names.
                            - Key Vault URI with confirmation that the connection string secret was seeded.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "setup-document-pipeline")]
    [Description("Provisions a document-processing pipeline: a Storage Account with input and output Blob containers, a Service Bus topic with a subscription for fan-out, and a Key Vault for API keys.")]
    [UsedImplicitly]
    public static IList<PromptMessage> SetupDocumentPipeline(
        [Description("Subscription ID containing the resource group.")]
        string subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Storage account name (lowercase, 3–24 chars).")]
        string storageAccountName,
        [Description("Blob container name for incoming documents.")]
        string inputContainerName,
        [Description("Blob container name for processed output documents.")]
        string outputContainerName,
        [Description("Service Bus namespace name.")]
        string serviceBusNamespaceName,
        [Description("Service Bus topic name for processing notifications.")]
        string topicName,
        [Description("Service Bus subscription name on the topic.")]
        string subscriptionName,
        [Description("Key Vault name for storing API keys.")]
        string keyVaultName,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId)
    {
        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Provision a document-processing pipeline in Topaz by performing these steps in order:

                            1. Call CreateStorageAccount with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", location="{location}", objectId="{objectId}". Note the returned connection string.
                            2. Call CreateBlobContainer with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", containerName="{inputContainerName}", objectId="{objectId}".
                            3. Call CreateBlobContainer with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", containerName="{outputContainerName}", objectId="{objectId}".
                            4. Call CreateServiceBusNamespace with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{serviceBusNamespaceName}", location="{location}", objectId="{objectId}".
                            5. Call CreateServiceBusTopic with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{serviceBusNamespaceName}", topicName="{topicName}", objectId="{objectId}".
                            6. Call CreateServiceBusSubscription with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{serviceBusNamespaceName}", topicName="{topicName}", subscriptionName="{subscriptionName}", objectId="{objectId}".
                            7. Call CreateKeyVault with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", keyVaultName="{keyVaultName}", location="{location}", objectId="{objectId}", secretName="StorageConnectionString", secretValue=<connection string from step 1>.

                            After all steps complete, provide:
                            - Storage connection string and URIs for both containers.
                            - Service Bus topic and subscription details.
                            - Key Vault URI.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "setup-event-ingestion")]
    [Description("Provisions an event ingestion stack: a Storage Account for capture, an Event Hub namespace with a hub, and a Key Vault seeded with the Event Hub connection string.")]
    [UsedImplicitly]
    public static IList<PromptMessage> SetupEventIngestion(
        [Description("Subscription ID containing the resource group.")]
        string subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Event Hub namespace name.")]
        string namespaceName,
        [Description("Event Hub name within the namespace.")]
        string eventHubName,
        [Description("Storage account name for event capture (lowercase, 3–24 chars).")]
        string storageAccountName,
        [Description("Blob container name for captured events.")]
        string captureContainerName,
        [Description("Key Vault name for storing the Event Hub connection string.")]
        string keyVaultName,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId,
        [Description("Number of Event Hub partitions (1–32). Defaults to 4.")]
        int partitionCount = 4)
    {
        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Provision an event ingestion stack in Topaz by performing these steps in order:

                            1. Call CreateStorageAccount with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", location="{location}", objectId="{objectId}".
                            2. Call CreateBlobContainer with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", containerName="{captureContainerName}", objectId="{objectId}".
                            3. Call CreateEventHubNamespace with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{namespaceName}", location="{location}", objectId="{objectId}". Note the returned connection string.
                            4. Call CreateEventHub with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", namespaceName="{namespaceName}", eventHubName="{eventHubName}", objectId="{objectId}", partitionCount={partitionCount}.
                            5. Call CreateKeyVault with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", keyVaultName="{keyVaultName}", location="{location}", objectId="{objectId}", secretName="EventHubConnectionString", secretValue=<connection string from step 3>.
                            6. Call GetConnectionStrings with subscriptionId="{subscriptionId}", objectId="{objectId}" to retrieve the full endpoint inventory.

                            After all steps complete, provide:
                            - Event Hub connection string and hub name with partition count.
                            - Capture storage connection string and container name.
                            - Key Vault URI.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "setup-container-registry-stack")]
    [Description("Provisions a Container Registry, a backing Storage Account, and a Key Vault seeded with the registry's admin credentials. Use this to test container-build or image-pull workflows locally.")]
    [UsedImplicitly]
    public static IList<PromptMessage> SetupContainerRegistryStack(
        [Description("Subscription ID containing the resource group.")]
        string subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Container Registry name (5–50 alphanumeric chars).")]
        string registryName,
        [Description("Storage account name for registry backing (lowercase, 3–24 chars).")]
        string storageAccountName,
        [Description("Key Vault name for storing registry credentials.")]
        string keyVaultName,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId,
        [Description("Registry SKU: Basic, Standard, or Premium. Defaults to Basic.")]
        string sku = "Basic")
    {
        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Provision a Container Registry stack in Topaz by performing these steps in order:

                            1. Call CreateContainerRegistry with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", registryName="{registryName}", location="{location}", objectId="{objectId}", sku="{sku}", adminUserEnabled=true. Note the returned loginServer, adminUsername, and adminPassword.
                            2. Call CreateStorageAccount with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", storageAccountName="{storageAccountName}", location="{location}", objectId="{objectId}".
                            3. Call CreateKeyVault with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", keyVaultName="{keyVaultName}", location="{location}", objectId="{objectId}", secretName="RegistryAdminPassword", secretValue=<adminPassword from step 1>.

                            After all steps complete, provide:
                            - Registry login server, admin username, and confirmation that the password was stored in Key Vault.
                            - Storage connection string.
                            - A ready-to-use docker login command: docker login <loginServer> -u <adminUsername> -p <stored-password>.
                            """,
                },
            },
        ];
    }
}

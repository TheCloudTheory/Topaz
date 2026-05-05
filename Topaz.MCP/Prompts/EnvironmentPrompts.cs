using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Topaz.MCP.Prompts;

[McpServerPromptType]
[Description("Prompts for starting, inspecting, and tearing down a Topaz emulator environment.")]
[UsedImplicitly]
public sealed class EnvironmentPrompts
{
    [McpServerPrompt(Name = "bootstrap-topaz")]
    [Description("First-time setup: starts the Topaz container, creates a subscription and a default resource group, then verifies the emulator is healthy. Use this as the entry point before provisioning any resources.")]
    [UsedImplicitly]
    public static IList<PromptMessage> BootstrapTopaz(
        [Description("Subscription ID to create (e.g. '10000000-0000-0000-0000-000000000001').")]
        string subscriptionId,
        [Description("Human-readable name for the subscription.")]
        string subscriptionName,
        [Description("Name of the initial resource group to create (e.g. 'rg-dev').")]
        string resourceGroupName,
        [Description("Azure location for the resource group (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId,
        [Description("Topaz Docker image tag to pull (e.g. 'v1.2.6-beta'). Defaults to 'v1.2.6-beta'.")]
        string version = "v1.2.6-beta")
    {
        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Bootstrap a fresh Topaz Azure emulator environment by performing these steps in order:

                            1. Call RunTopazAsContainer with version="{version}" to start the emulator container.
                            2. Call CreateSubscription with subscriptionId="{subscriptionId}", subscriptionName="{subscriptionName}", objectId="{objectId}".
                            3. Call CreateResourceGroup with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", location="{location}", objectId="{objectId}".
                            4. Call GetTopazStatus to verify the emulator is running and confirm which services are reachable.

                            After all steps complete, provide a summary that includes:
                            - The subscription ID and resource group that were created.
                            - The overall emulator status and a list of which services are up or down.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "inspect-environment")]
    [Description("Audits the running Topaz instance: checks health, lists subscriptions, and returns connection strings for all provisioned resources. Use this to get a complete picture of the current emulator state.")]
    [UsedImplicitly]
    public static IList<PromptMessage> InspectEnvironment(
        [Description("Subscription ID to inspect.")]
        string subscriptionId,
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
                            Audit the running Topaz emulator by performing these steps:

                            1. Call GetTopazStatus to check overall health and confirm which service ports are reachable.
                            2. Call ListSubscriptions with objectId="{objectId}" to list all registered subscriptions.
                            3. Call GetConnectionStrings with subscriptionId="{subscriptionId}", objectId="{objectId}" to retrieve connection strings and URIs for every provisioned resource.

                            Present the results as a structured report with three sections:
                            - Emulator status (version, working directory, services up/down).
                            - Subscriptions list.
                            - Resource inventory grouped by resource type, with ready-to-use connection strings.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "teardown-environment")]
    [Description("Tears down Topaz resources: deletes a resource group and optionally stops the container. Use this to clean up after a local development or testing session.")]
    [UsedImplicitly]
    public static IList<PromptMessage> TeardownEnvironment(
        [Description("Subscription ID containing the resource group to delete.")]
        string subscriptionId,
        [Description("Name of the resource group to delete.")]
        string resourceGroupName,
        [Description("Object ID of the acting user. Pass '00000000-0000-0000-0000-000000000000' for superadmin.")]
        string objectId,
        [Description("Whether to stop the Topaz container after deleting the resource group. Defaults to false.")]
        bool stopContainer = false)
    {
        var containerStep = stopContainer
            ? "\n3. Call StopTopazContainer to stop and remove the emulator container."
            : "\nSkip stopping the container — it will remain running for other resource groups.";

        return
        [
            new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock
                {
                    Text = $"""
                            Tear down the Topaz environment by performing these steps:

                            1. Call DeleteResourceGroup with subscriptionId="{subscriptionId}", resourceGroupName="{resourceGroupName}", objectId="{objectId}" to delete the resource group and all resources it contains.
                            2. Confirm the deletion was successful (Deleted=true).{containerStep}

                            After all steps complete, confirm what was deleted and whether the emulator is still running.
                            """,
                },
            },
        ];
    }

    [McpServerPrompt(Name = "setup-multi-tenant-fixtures")]
    [Description("Creates isolated per-tenant subscriptions, resource groups, storage accounts, and Key Vaults following a naming convention. Use this to simulate multi-tenant isolation in local tests.")]
    [UsedImplicitly]
    public static IList<PromptMessage> SetupMultiTenantFixtures(
        [Description("Comma-separated list of tenant names to provision (e.g. 'acme,globex,initech').")]
        string tenantNames,
        [Description("Naming prefix applied to all resource names (e.g. 'dev' produces 'dev-acme-rg', 'devacmestorage').")]
        string namingPrefix,
        [Description("Azure location for all resources (e.g. 'westeurope').")]
        string location,
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
                            Provision isolated Topaz fixtures for each tenant in this list: {tenantNames}.
                            Use the naming prefix "{namingPrefix}" and location "{location}".
                            Use objectId="{objectId}" for all operations.

                            For each tenant name T, perform these steps in order (replace T with the actual tenant name):
                            1. Generate a deterministic subscription ID for tenant T (e.g. derive a UUID from the tenant name or use an incrementing suffix starting from '20000000-0000-0000-0000-000000000001').
                            2. Call CreateSubscription with the generated subscriptionId, subscriptionName="{namingPrefix}-T", objectId="{objectId}".
                            3. Call CreateResourceGroup with the generated subscriptionId, resourceGroupName="{namingPrefix}-T-rg", location="{location}", objectId="{objectId}".
                            4. Call CreateStorageAccount with the generated subscriptionId, resourceGroupName="{namingPrefix}-T-rg", storageAccountName="{namingPrefix}Tstorage" (lowercase, max 24 chars), location="{location}", objectId="{objectId}".
                            5. Call CreateKeyVault with the generated subscriptionId, resourceGroupName="{namingPrefix}-T-rg", keyVaultName="{namingPrefix}-T-kv", location="{location}", objectId="{objectId}".

                            After provisioning all tenants, provide a table listing each tenant's subscription ID, resource group, storage connection string, and Key Vault URI.
                            """,
                },
            },
        ];
    }
}

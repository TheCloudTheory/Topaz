using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure SQL Server resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateSqlServerTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a SQL Server in the given resource group.")]
    [UsedImplicitly]
    public static async Task<SqlServerResult> CreateSqlServer(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the SQL Server will be created.")]
        string resourceGroupName,
        [Description("Name of the SQL Server to create.")]
        string serverName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Administrator login username.")]
        string administratorLogin,
        [Description("Administrator login password.")]
        string administratorLoginPassword)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var serverData = new SqlServerData(new AzureLocation(location))
        {
            AdministratorLogin = administratorLogin,
            AdministratorLoginPassword = administratorLoginPassword,
            Version = "12.0"
        };

        var result = await resourceGroup.Value.GetSqlServers()
            .CreateOrUpdateAsync(WaitUntil.Completed, serverName, serverData)
            .ConfigureAwait(false);

        return new SqlServerResult
        {
            Name = result.Value.Data.Name,
            FullyQualifiedDomainName = result.Value.Data.FullyQualifiedDomainName ?? string.Empty
        };
    }

    public sealed record SqlServerResult
    {
        public required string Name { [UsedImplicitly] get; init; }
        public required string FullyQualifiedDomainName { [UsedImplicitly] get; init; }
    }
}

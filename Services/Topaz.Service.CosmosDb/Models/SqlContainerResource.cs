using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlContainerResource : ArmSubresource<SqlContainerResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SqlContainerResource()
#pragma warning restore CS8618
    {
    }

    public SqlContainerResource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        string containerName,
        SqlContainerResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases/{databaseName}/containers/{containerName}";
        Name = containerName;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers";
    public override SqlContainerResourceProperties Properties { get; init; }

    public string GetAccountName() => Id.Split("/")[8];
    public string GetDatabaseName() => Id.Split("/")[10];
}

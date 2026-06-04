using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.CosmosDb.Models;

public sealed class SqlDatabaseResource : ArmSubresource<SqlDatabaseResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public SqlDatabaseResource()
#pragma warning restore CS8618
    {
    }

    public SqlDatabaseResource(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string accountName,
        string databaseName,
        SqlDatabaseResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/sqlDatabases/{databaseName}";
        Name = databaseName;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.DocumentDB/databaseAccounts/sqlDatabases";
    public override SqlDatabaseResourceProperties Properties { get; init; }

    public string GetAccountName() => Id.Split("/")[8];
}

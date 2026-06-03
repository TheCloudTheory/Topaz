using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class DatabaseAccountConnectionString
{
    public string? ConnectionString { get; set; }
    public string? Description { get; set; }
    public string? KeyKind { get; set; }
    public string? Type { get; set; }
}

public sealed class DatabaseAccountConnectionStringsResponse
{
    public DatabaseAccountConnectionString[] ConnectionStrings { get; set; } = [];

    public static DatabaseAccountConnectionStringsResponse FromKeys(
        string endpoint,
        string primaryKey,
        string secondaryKey,
        string primaryReadonlyKey,
        string secondaryReadonlyKey) =>
        new()
        {
            ConnectionStrings =
            [
                new DatabaseAccountConnectionString
                {
                    ConnectionString = $"AccountEndpoint={endpoint};AccountKey={primaryKey};",
                    Description = "Primary SQL Connection String",
                    KeyKind = "Primary",
                    Type = "Sql"
                },
                new DatabaseAccountConnectionString
                {
                    ConnectionString = $"AccountEndpoint={endpoint};AccountKey={secondaryKey};",
                    Description = "Secondary SQL Connection String",
                    KeyKind = "Secondary",
                    Type = "Sql"
                },
                new DatabaseAccountConnectionString
                {
                    ConnectionString = $"AccountEndpoint={endpoint};AccountKey={primaryReadonlyKey};",
                    Description = "Primary Read-Only SQL Connection String",
                    KeyKind = "PrimaryReadonly",
                    Type = "Sql"
                },
                new DatabaseAccountConnectionString
                {
                    ConnectionString = $"AccountEndpoint={endpoint};AccountKey={secondaryReadonlyKey};",
                    Description = "Secondary Read-Only SQL Connection String",
                    KeyKind = "SecondaryReadonly",
                    Type = "Sql"
                }
            ]
        };

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

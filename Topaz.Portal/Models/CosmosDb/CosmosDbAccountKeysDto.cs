namespace Topaz.Portal.Models.CosmosDb;

public sealed class CosmosDbAccountKeysDto
{
    public string PrimaryConnectionString { get; set; } = "";
    public string SecondaryConnectionString { get; set; } = "";
    public string PrimaryKey { get; set; } = "";
    public string SecondaryKey { get; set; } = "";
    public string AccountEndpoint { get; set; } = "";
}

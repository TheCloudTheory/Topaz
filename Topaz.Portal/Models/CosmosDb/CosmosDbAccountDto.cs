namespace Topaz.Portal.Models.CosmosDb;

public sealed class CosmosDbAccountDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public string? ResourceGroupName { get; set; }
    public string? SubscriptionId { get; set; }
    public string? SubscriptionName { get; set; }
    public string? Kind { get; set; }
    public string? DocumentEndpoint { get; set; }
    public string? ProvisioningState { get; set; }
    public Dictionary<string, string> Tags { get; set; } = [];
}

public sealed class ListCosmosDbAccountsResponse
{
    public CosmosDbAccountDto[] Value { get; set; } = [];
}

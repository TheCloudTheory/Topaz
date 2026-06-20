namespace Topaz.Service.CosmosDb.Models;

public sealed class ContainerPartitionKey
{
    public string[] Paths { get; set; } = [];
    public string Kind { get; set; } = "Hash";
    public int? Version { get; set; } = 2;
}

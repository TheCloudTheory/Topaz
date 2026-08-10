namespace Topaz.Importer;

internal sealed class SeedResourcesRequest
{
    public string? SubscriptionId { get; set; }
    public string? ResourceGroup { get; set; }
    public string? ResourceType { get; set; }
    public bool DryRun { get; set; }
    public bool Overwrite { get; set; }
}
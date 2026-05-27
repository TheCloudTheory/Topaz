namespace Topaz.ResourceManager;

public record ResourceSku
{
    public string? Name { get; init; }
    public string? Tier { get; init; }
    public string? Family { get; init; }
}
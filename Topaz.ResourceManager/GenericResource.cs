using Azure.ResourceManager;

namespace Topaz.ResourceManager;

public sealed class GenericResource : ArmResource<object>
{
    public override string Id { get; init; } = null!;
    public override string Name { get; init; } = null!;
    public override string Type { get; } = null!;
    public override string Location { get; init; } = null!;
    public override IDictionary<string, string> Tags { get;  } = null!;
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override object Properties { get; init; } = null!;

    public T? As<T, TProps>() where T : ArmResource<TProps>, new()
    {
        return this as T;
    }
}
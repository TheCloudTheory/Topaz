using System.Text.Json;

namespace Topaz.ResourceManager;

public sealed class GenericResource : ArmResource<object>
{
    public override string Id { get; init; } = null!;
    public override string Name { get; init; } = null!;
    public override string Type => null!;
    public override string Location { get; set; } = null!;
    public override IDictionary<string, string> Tags { get; set; } = null!;
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override object Properties { get; init; } = null!;

    public T? As<T, TProps>() 
        where T : ArmResource<TProps>, new()
        where TProps : new()
    {
        var result = new T
        {
            Id = Id,
            Name = Name,
            Location = Location,
            Sku = Sku,
            Kind = Kind,
            Properties = ConvertProperties<TProps>(Properties)
        };
        
        return result;
    }

    private static TProps ConvertProperties<TProps>(object source) where TProps : new()
    {
        return JsonSerializer.Deserialize<TProps>(JsonSerializer.Serialize(source))!;
    }
}
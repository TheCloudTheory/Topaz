using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public sealed class GenericResource : ArmResource<object>
{
    public override string Id { get; init; } = null!;
    public override string Name { get; init; } = null!;
    public override string Type { get; init; } = null!;
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
            Tags = Tags,
            Properties = ConvertProperties<TProps>(Properties)
        };
        
        return result;
    }
    
    public T? AsSubresource<T, TProps>() 
        where T : ArmSubresource<TProps>, new()
        where TProps : new()
    {
        var result = new T
        {
            Id = Id,
            Name = Name,
            Properties = ConvertProperties<TProps>(Properties)
        };
        
        return result;
    }

    private static TProps ConvertProperties<TProps>(object source) where TProps : new()
    {
        // Use GlobalSettings.JsonOptions for both serialize and deserialize to ensure
        // camelCase JSON property names (from ARM templates) correctly map to PascalCase
        // C# properties via PropertyNameCaseInsensitive = true.
        // Strip unresolved ARM expressions before deserializing so that fields like
        // "[tenant().tenantId]" don't cause conversion failures on Guid/int properties.
        var node = JsonNode.Parse(JsonSerializer.Serialize(source, GlobalSettings.JsonOptions));
        StripArmExpressions(node);
        return JsonSerializer.Deserialize<TProps>(node?.ToJsonString() ?? "{}", GlobalSettings.JsonOptions)!;
    }

    /// <summary>
    /// Recursively removes any JSON object property whose value is an unresolved ARM template
    /// expression (a string beginning with '[' and ending with ']'). This prevents type-conversion
    /// failures when ARM functions such as <c>tenant().tenantId</c> have not been evaluated by the
    /// template engine.
    /// </summary>
    private static void StripArmExpressions(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var toRemove = new List<string>();
            foreach (var (key, value) in obj)
            {
                if (value is JsonValue v && v.TryGetValue<string>(out var s)
                    && s.StartsWith('[') && s.EndsWith(']'))
                    toRemove.Add(key);
                else
                    StripArmExpressions(value);
            }
            foreach (var key in toRemove)
                obj.Remove(key);
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
                StripArmExpressions(item);
        }
    }
}
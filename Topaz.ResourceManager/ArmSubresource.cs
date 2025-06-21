using System.Text.Json;
using Topaz.Shared;

namespace Topaz.ResourceManager;

public abstract class ArmSubresource<T>
{
    public abstract string Id { get; init; }
    public abstract string Name { get; init; }
    public abstract string Type { get; }
    public abstract T Properties { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
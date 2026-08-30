namespace Topaz.Service.Shared.Models;

public class ResourcesListResultResponseBase<TResource> : TopazApiModel where TResource : class, new()
{
    public string? NextLink { get; set; } = "";
    public TResource[] Value { get; set; } = [];

    public static ResourcesListResultResponseBase<TResource> From(TResource[] value)
    {
        return new ResourcesListResultResponseBase<TResource>
        {
            Value = value
        };
    }
}
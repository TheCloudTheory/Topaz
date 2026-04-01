namespace Topaz.Service.ResourceGroup.Models.Requests;

public record UpdateResourceGroupRequest
{
    public IDictionary<string, string>? Tags { get; set; }
    public string? ManagedBy { get; set; }
}

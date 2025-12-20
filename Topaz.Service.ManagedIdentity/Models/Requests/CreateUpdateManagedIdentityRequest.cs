namespace Topaz.Service.ManagedIdentity.Models.Requests;

public sealed class CreateUpdateManagedIdentityRequest
{
    public required string Location { get; set; }
    
    public Dictionary<string, string>? Tags { get; set; }
    
    public ManagedIdentityProperties? Properties { get; set; }
    
    public sealed class ManagedIdentityProperties
    {
        public string? IsolationScope { get; set; }
    }
}
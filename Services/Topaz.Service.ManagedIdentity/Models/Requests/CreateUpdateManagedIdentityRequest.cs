namespace Topaz.Service.ManagedIdentity.Models.Requests;

public sealed class CreateUpdateManagedIdentityRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public ManagedIdentityProperties? Properties { get; init; }
    
    public sealed class ManagedIdentityProperties
    {
        public string? IsolationScope { get; init; }
    }
}
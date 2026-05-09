namespace Topaz.Portal.Models.ManagedIdentities;

public sealed class FederatedCredentialDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Issuer { get; init; }
    public string? Subject { get; init; }
    public IReadOnlyList<string> Audiences { get; init; } = [];
}

public sealed class ListFederatedCredentialsResponse
{
    public FederatedCredentialDto[] Value { get; init; } = [];
}

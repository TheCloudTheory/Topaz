using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ManagedIdentity.Models.Responses;

public sealed class FederatedIdentityCredentialsListResponse
{
    public FederatedIdentityCredentialResource[] Value { get; init; } = [];

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

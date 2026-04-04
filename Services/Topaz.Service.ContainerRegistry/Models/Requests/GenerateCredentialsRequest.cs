using JetBrains.Annotations;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class GenerateCredentialsRequest
{
    public string? TokenId { get; set; }
    public DateTimeOffset? Expiry { get; set; }
    public string? Name { get; set; }
}

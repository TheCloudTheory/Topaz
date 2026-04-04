using JetBrains.Annotations;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class RegenerateCredentialRequest
{
    /// <summary>Password to regenerate: "password" or "password2".</summary>
    public string? Name { get; set; }
}

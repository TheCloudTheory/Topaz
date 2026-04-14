using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models.Requests;

[UsedImplicitly]
internal sealed class RegenerateStorageAccountKeyRequest
{
    public string? KeyName { get; set; }
}

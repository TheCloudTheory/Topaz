using System.Text.Json;
using JetBrains.Annotations;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class UpdateAcrTaskRequest
{
    public IDictionary<string, string>? Tags { get; init; }
    public JsonElement? Identity { get; init; }
    public UpdateAcrTaskRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    internal sealed class UpdateAcrTaskRequestProperties
    {
        public string? Status { get; init; }
        public int? Timeout { get; init; }
        public JsonElement? Platform { get; init; }
        public JsonElement? AgentConfiguration { get; init; }
        public JsonElement? Step { get; init; }
        public JsonElement? Trigger { get; init; }
        public JsonElement? Credentials { get; init; }
    }
}

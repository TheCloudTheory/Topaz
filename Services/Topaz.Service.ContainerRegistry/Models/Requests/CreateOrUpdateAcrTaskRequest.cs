using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class CreateOrUpdateAcrTaskRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public JsonElement? Identity { get; init; }
    public AcrTaskRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    internal sealed class AcrTaskRequestProperties
    {
        public string? Status { get; init; }
        public int? Timeout { get; init; }
        public AcrTaskPlatformProperties? Platform { get; init; }
        public AcrTaskAgentProperties? AgentConfiguration { get; init; }
        public AcrTaskStepProperties? Step { get; init; }
        public AcrTaskTriggerProperties? Trigger { get; init; }
        public AcrTaskCredentials? Credentials { get; init; }
    }
}

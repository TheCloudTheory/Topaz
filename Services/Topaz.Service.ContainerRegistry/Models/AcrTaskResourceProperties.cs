using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;

namespace Topaz.Service.ContainerRegistry.Models;

[UsedImplicitly]
internal sealed class AcrTaskResourceProperties
{
    public string? Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState ProvisioningState { get; set; } = ProvisioningState.Succeeded;
    public DateTimeOffset CreationDate { get; set; }
    public int Timeout { get; set; } = 3600;
    public AcrTaskPlatformProperties? Platform { get; set; }
    public AcrTaskAgentProperties? AgentConfiguration { get; set; }
    public AcrTaskStepProperties? Step { get; set; }
    public AcrTaskTriggerProperties? Trigger { get; set; }
    public AcrTaskCredentials? Credentials { get; set; }

    public static AcrTaskResourceProperties FromRequest(CreateOrUpdateAcrTaskRequest request) =>
        new()
        {
            Status = request.Properties?.Status ?? "Enabled",
            ProvisioningState = ProvisioningState.Succeeded,
            CreationDate = DateTimeOffset.UtcNow,
            Timeout = request.Properties?.Timeout ?? 3600,
            Platform = request.Properties?.Platform,
            AgentConfiguration = request.Properties?.AgentConfiguration,
            Step = request.Properties?.Step,
            Trigger = request.Properties?.Trigger,
            Credentials = request.Properties?.Credentials
        };

    public static void UpdateFromRequest(AcrTaskResource resource, UpdateAcrTaskRequest request)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (request.Properties == null) return;

        if (request.Properties.Status != null)
            resource.Properties.Status = request.Properties.Status;

        if (request.Properties.Timeout.HasValue)
            resource.Properties.Timeout = request.Properties.Timeout.Value;

        if (request.Properties.Platform != null)
            resource.Properties.Platform = request.Properties.Platform;

        if (request.Properties.AgentConfiguration != null)
            resource.Properties.AgentConfiguration = request.Properties.AgentConfiguration;

        if (request.Properties.Step != null)
            resource.Properties.Step = request.Properties.Step;

        if (request.Properties.Trigger != null)
            resource.Properties.Trigger = request.Properties.Trigger;

        if (request.Properties.Credentials != null)
            resource.Properties.Credentials = request.Properties.Credentials;

        if (request.Tags != null)
            resource.Tags = request.Tags;

        if (request.Identity.HasValue)
            resource.Identity = request.Identity;
    }
}

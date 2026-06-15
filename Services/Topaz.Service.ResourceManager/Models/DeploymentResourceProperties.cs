using System.Text;
using System.Text.Json;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models;

/// <summary>
/// Serializable error detail for a failed ARM deployment.
/// Matches the { "code": "...", "message": "..." } shape expected by ARM clients.
/// </summary>
public sealed class DeploymentErrorInfo
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}

public sealed class DeploymentResourceProperties
{
    public string DebugSettingDetailLevel => "none";
    public IReadOnlyList<SubResource>? OutputResources { get; set; }
    public IReadOnlyList<SubResource>? ValidatedResources { get; set; }
    public string? ProvisioningState { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public JsonElement? Outputs { get; set; }
    public IReadOnlyList<ResourceProviderData>? Providers { get; set;}
    public IReadOnlyList<ArmDependency>? Dependencies { get; set; }
    public ArmDeploymentTemplateLink? TemplateLink { get; set; }
    public JsonElement? Parameters { get; set; }
    public ArmDeploymentParametersLink? ParametersLink { get; set; }
    public IReadOnlyList<ArmDeploymentExtensionDefinition>? Extensions { get; set; }
    public string? Mode { get; set; }
    public ErrorDeploymentExtended? ErrorDeployment { get; set; }
    public string? TemplateHash { get; set; }
    public IReadOnlyList<ArmResourceReference>? OutputResourceDetails { get; set; }
    public IReadOnlyList<ArmResourceReference>? ValidatedResourceDetails { get; set; }
    public DeploymentErrorInfo? Error { get; set; }
    public IReadOnlyList<DeploymentDiagnosticsDefinition>? Diagnostics { get; set; }
    public ValidationLevel? ValidationLevel { get; set; }

    internal static DeploymentResourceProperties New(string deploymentMode, string template,
        Dictionary<string, CreateDeploymentRequest.ParameterValue>? parameters)
    {
        return new DeploymentResourceProperties
        {
            CorrelationId = Guid.NewGuid().ToString(),
            Mode = deploymentMode,
            TemplateHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(template)),
            ProvisioningState = ResourcesProvisioningState.Running.ToString(),
            Parameters = parameters == null ? null : JsonSerializer.SerializeToElement(parameters, GlobalSettings.JsonOptions),
            Timestamp = DateTimeOffset.UtcNow,
        };
    }

    internal static DeploymentResourceProperties ForValidate(CreateDeploymentRequest request)
    {
        return new DeploymentResourceProperties
        {
            CorrelationId = Guid.NewGuid().ToString(),
            Mode = request.Properties?.Mode ?? "Incremental",
            ProvisioningState = ResourcesProvisioningState.Succeeded.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Parameters = request.Properties?.GetParameterValues() is { } parameters
                ? JsonSerializer.SerializeToElement(parameters, GlobalSettings.JsonOptions)
                : null
        };
    }
}
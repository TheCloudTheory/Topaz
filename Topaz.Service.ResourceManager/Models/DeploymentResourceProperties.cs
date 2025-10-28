using Azure;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace Topaz.Service.ResourceManager.Models;

public sealed class DeploymentResourceProperties
{
    public string DebugSettingDetailLevel => "none";
    public IReadOnlyList<SubResource>? OutputResources { get; set; }
    public IReadOnlyList<SubResource>? ValidatedResources { get; set; }
    public string ProvisioningState => ResourcesProvisioningState.Succeeded.ToString();
    public string? CorrelationId { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public BinaryData? Outputs { get; set; }
    public IReadOnlyList<ResourceProviderData>? Providers { get; set;}
    public IReadOnlyList<ArmDependency>? Dependencies { get; set; }
    public ArmDeploymentTemplateLink? TemplateLink { get; set; }
    public BinaryData? Parameters { get; set; }
    public ArmDeploymentParametersLink? ParametersLink { get; set; }
    public IReadOnlyList<ArmDeploymentExtensionDefinition>? Extensions { get; set; }
    public string? Mode { get; set; }
    public ErrorDeploymentExtended? ErrorDeployment { get; set; }
    public string? TemplateHash { get; set; }
    public IReadOnlyList<ArmResourceReference>? OutputResourceDetails { get; set; }
    public IReadOnlyList<ArmResourceReference>? ValidatedResourceDetails { get; set; }
    public ResponseError? Error { get; set; }
    public IReadOnlyList<DeploymentDiagnosticsDefinition>? Diagnostics { get; set; }
    public ValidationLevel? ValidationLevel { get; set; }
}
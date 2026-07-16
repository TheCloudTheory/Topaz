using Azure.Deployments.Core.Definitions.Schema;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json.Linq;
using Topaz.Service.ResourceManager.Models;

namespace Topaz.Service.ResourceManager.Deployment;

/// <summary>
/// Represents a queued template deployment job at any ARM scope (resource group or subscription).
/// Lifecycle callbacks and persistence are injected as delegates, keeping the orchestrator
/// scope-agnostic.
/// </summary>
internal sealed class TemplateDeployment(
    string id,
    string name,
    Template template,
    Action complete,
    Action cancel,
    Action fail,
    Action persist,
    Action<System.Text.Json.JsonElement?> setOutputs,
    InsensitiveDictionary<JToken> metadata,
    System.Text.Json.JsonElement? parameters,
    Action<DeploymentErrorInfo?>? setError = null)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public Template Template { get; } = template;
    public DeploymentStatus Status { get; private set; } = DeploymentStatus.New;
    public CancellationToken CancellationToken => _cts?.Token ?? CancellationToken.None;
    public InsensitiveDictionary<JToken> Metadata { get; } = metadata;
    public System.Text.Json.JsonElement? Parameters { get; } = parameters;

    /// <summary>
    /// Outputs from nested module deployments, keyed by deployment resource name.
    /// Populated by <c>HandleNestedDeployment</c> so that subscription-scope output
    /// expressions such as <c>reference('foundation-deployment').outputs.x.value</c>
    /// can be resolved without a filesystem round-trip.
    /// </summary>
    public Dictionary<string, System.Text.Json.JsonElement?> NestedDeploymentOutputs { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps Bicep symbolic resource names (dict-key in newer ARM templates) to their
    /// resource type, enabling resolution of <c>reference('symbolicName')</c> expressions
    /// in output evaluation.  Populated by <c>HandleNestedDeployment</c> when the inner
    /// template uses the dict-style resources format.
    /// </summary>
    public Dictionary<string, string> SymbolicNameMap { get; } = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _cts;

    private readonly Action<DeploymentErrorInfo?> _setError = setError ?? (_ => { });

    public void SetCancellationTokenSource(CancellationTokenSource cts) => _cts = cts;

    public void Start() => Status = DeploymentStatus.Running;

    public void Complete()
    {
        complete();
        Status = DeploymentStatus.Completed;
    }

    public void Cancel()
    {
        cancel();
        Status = DeploymentStatus.Cancelled;
    }

    public void Fail()
    {
        fail();
        Status = DeploymentStatus.Failed;
    }

    public void Persist() => persist();

    public void SetOutputs(System.Text.Json.JsonElement? outputs) => setOutputs(outputs);

    public void SetError(DeploymentErrorInfo? error) => _setError(error);

    public enum DeploymentStatus
    {
        New,
        Running,
        Completed,
        Cancelled,
        Failed,
    }
}
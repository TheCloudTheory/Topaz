using Azure.Deployments.Core.Definitions.Schema;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json.Linq;

namespace Topaz.Service.ResourceManager.Deployment;

/// <summary>
/// Represents a queued template deployment job at any ARM scope (resource group or subscription).
/// Lifecycle callbacks and persistence are injected as delegates, keeping the orchestrator
/// scope-agnostic.
/// </summary>
internal sealed class TemplateDeployment
{
    public string Id { get; }
    public string Name { get; }
    public Template Template { get; }
    public DeploymentStatus Status { get; private set; } = DeploymentStatus.New;
    public CancellationToken CancellationToken => _cts?.Token ?? CancellationToken.None;
    public InsensitiveDictionary<JToken> Metadata { get; }
    public System.Text.Json.JsonElement? Parameters { get; }

    private CancellationTokenSource? _cts;

    private readonly Action _complete;
    private readonly Action _cancel;
    private readonly Action _fail;
    private readonly Action _persist;
    private readonly Action<BinaryData?> _setOutputs;

    public TemplateDeployment(
        string id,
        string name,
        Template template,
        Action complete,
        Action cancel,
        Action fail,
        Action persist,
        Action<BinaryData?> setOutputs,
        InsensitiveDictionary<JToken> metadata,
        System.Text.Json.JsonElement? parameters)
    {
        Id = id;
        Name = name;
        Template = template;
        _complete = complete;
        _cancel = cancel;
        _fail = fail;
        _persist = persist;
        _setOutputs = setOutputs;
        Metadata = metadata;
        Parameters = parameters;
    }

    public void SetCancellationTokenSource(CancellationTokenSource cts) => _cts = cts;

    public void Start() => Status = DeploymentStatus.Running;

    public void Complete()
    {
        _complete();
        Status = DeploymentStatus.Completed;
    }

    public void Cancel()
    {
        _cancel();
        Status = DeploymentStatus.Cancelled;
    }

    public void Fail()
    {
        _fail();
        Status = DeploymentStatus.Failed;
    }

    public void Persist() => _persist();

    public void SetOutputs(BinaryData? outputs) => _setOutputs(outputs);

    public enum DeploymentStatus
    {
        New,
        Running,
        Completed,
        Cancelled,
        Failed,
    }
}
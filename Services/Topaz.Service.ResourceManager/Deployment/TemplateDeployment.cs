using Azure.Deployments.Core.Definitions.Schema;

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

    private CancellationTokenSource? _cts;

    private readonly Action _complete;
    private readonly Action _cancel;
    private readonly Action _fail;
    private readonly Action _persist;

    public TemplateDeployment(
        string id,
        string name,
        Template template,
        Action complete,
        Action cancel,
        Action fail,
        Action persist)
    {
        Id = id;
        Name = name;
        Template = template;
        _complete = complete;
        _cancel = cancel;
        _fail = fail;
        _persist = persist;
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

    public enum DeploymentStatus
    {
        New,
        Running,
        Completed,
        Cancelled,
        Failed,
    }
}
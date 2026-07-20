using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Host.Models;

internal sealed record HealthResponse([UsedImplicitly] string WorkingDirectory, string Version, bool ChaosEnabled = false)
{
    [UsedImplicitly] public string Status => "Healthy";
    [UsedImplicitly] public string RunningMode => HostState.IsRunningInsideContainer ? "Container" : "Standalone";
    [UsedImplicitly] public bool HttpsConnectProxyAvailable => HostState.HttpsConnectProxyAvailable;
    [UsedImplicitly] public bool AcrDockerExecutorAvailable => HostState.AcrDockerExecutorAvailable;

    [UsedImplicitly]
    public IReadOnlyCollection<BackgroundServiceHealthResponse> BackgroundServices => BackgroundServiceOrchestrator
        .Services.Select(s => new BackgroundServiceHealthResponse(s.Name, s.ExecutedAt)).ToList();

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

internal sealed record BackgroundServiceHealthResponse([UsedImplicitly] string Name, DateTimeOffset? ExecutedAt);
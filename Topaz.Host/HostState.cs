using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.ContainerRegistry.Executors;
using Topaz.Shared;

namespace Topaz.Host;

[UsedImplicitly]
internal sealed class HostState(ITopazLogger logger)
{
    public bool AcrDockerExecutorAvailable => new AcrDockerExecutor(logger).IsAvailable();
    public static bool IsRunningInsideContainer  => Environment.GetEnvironmentVariable("TOPAZ_CONTAINERIZED") == "true";
    public static bool HttpsConnectProxyAvailable { get; set; }
}
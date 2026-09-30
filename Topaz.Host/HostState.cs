using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.ContainerRegistry.Executors;

namespace Topaz.Host;

[UsedImplicitly]
internal sealed class HostState
{
    public static bool AcrDockerExecutorAvailable => AcrDockerExecutor.IsAvailable();
    public static bool IsRunningInsideContainer  => Environment.GetEnvironmentVariable("TOPAZ_CONTAINERIZED") == "true";
    public static bool HttpsConnectProxyAvailable { get; set; }
}
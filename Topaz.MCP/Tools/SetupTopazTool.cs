using System.ComponentModel;
using DotNet.Testcontainers.Builders;
using JetBrains.Annotations;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Topaz.Shared;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Helps in setting up Topaz locally.")]
[UsedImplicitly]
public class SetupTopazTool
{
    private static DotNet.Testcontainers.Containers.IContainer? _container;
    
    [McpServerTool]
    [Description("Runs and configures Topaz locally as a container")]
    [UsedImplicitly]
    public static async Task RunTopazAsContainer(
        [Description("Configures log level to be used by Topaz")]
        LogLevel logLevel = LogLevel.Information,
        [Description("Image tag to use when running the emulator")]
        string version = "v1.2.6-beta")
    {
        _container = new ContainerBuilder()
            .WithImage($"thecloudtheory/topaz-host:{version}")
            .WithPortBinding(GlobalSettings.DefaultEventHubAmqpPort)
            .WithPortBinding(GlobalSettings.DefaultServiceBusAmqpPort)
            .WithPortBinding(GlobalSettings.AdditionalServiceBusPort)
            .WithPortBinding(GlobalSettings.DefaultTableStoragePort)
            .WithPortBinding(GlobalSettings.DefaultBlobStoragePort)
            .WithPortBinding(8893) // DefaultQueueStoragePort
            .WithPortBinding(8894) // DefaultFileStoragePort
            .WithPortBinding(GlobalSettings.DefaultEventHubPort)
            .WithPortBinding(GlobalSettings.DefaultKeyVaultPort)
            .WithPortBinding(GlobalSettings.DefaultResourceManagerPort)
            .WithPortBinding(8892) // ContainerRegistryPort
            .WithName("topaz.local.dev")
            .WithCommand("--log-level", logLevel.ToString())
            .Build();

        await _container.StartAsync()
            .ConfigureAwait(false);
    }
    
    [McpServerTool]
    [Description("Stops Topaz which was previously run as container.")]
    [UsedImplicitly]
    public static async Task StopTopazContainer()
    {
        if (_container == null) throw new McpException("Can't stop Topaz if it wasn't started.");
        
        await _container.StopAsync()
            .ConfigureAwait(false);

        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
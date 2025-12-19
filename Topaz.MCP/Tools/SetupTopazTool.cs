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
        string version = "v1.0.299-alpha")
    {
        _container = new ContainerBuilder()
            .WithImage($"thecloudtheory/topaz-cli:{version}")
            .WithPortBinding(8890)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8897)
            .WithPortBinding(8891)
            .WithName("topaz.local.dev")
            .WithCommand("start", "--skip-dns-registration", "--log-level", logLevel.ToString())
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
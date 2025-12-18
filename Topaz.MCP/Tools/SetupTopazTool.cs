using System.ComponentModel;
using DotNet.Testcontainers.Builders;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Shared;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Helps in setting up Topaz locally.")]
[UsedImplicitly]
public class SetupTopazTool
{
    [McpServerTool]
    [Description("Runs and configures Topaz locally as a container")]
    [UsedImplicitly]
    public async Task RunTopazAsContainer(
        [Description("Configures log level to be used by Topaz")]
        LogLevel logLevel = LogLevel.Information,
        [Description("Image tag to use when running the emulator")]
        string version = "v1.0.299-alpha")
    {
        var container = new ContainerBuilder()
            .WithImage($"thecloudtheory/topaz-cli:{version}")
            .WithPortBinding(8890)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8897)
            .WithPortBinding(8891)
            .WithName("topaz.local.dev")
            .WithCommand("start", "--skip-dns-registration", "--log-level", logLevel.ToString())
            .Build();

        await container.StartAsync()
            .ConfigureAwait(false);
    }
}
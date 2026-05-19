using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Shared;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Helps in setting up Topaz locally.")]
[UsedImplicitly]
public class SetupTopazTool
{
    private const string ContainerName = "topaz.local.dev";

    [McpServerTool]
    [Description(
        "Returns a Docker CLI command that downloads and starts the Topaz emulator container. " +
        "Run the returned command in a terminal that has Docker available.")]
    [UsedImplicitly]
    public static string RunTopazAsContainer(
        [Description("Log level to be used by Topaz (e.g. Information, Debug, Warning)")]
        LogLevel logLevel = LogLevel.Information,
        [Description("Image tag to use when running the emulator")]
        string version = "v1.4.101-beta",
        [Description(
            "Docker platform to pull and run (e.g. 'linux/arm64' for Apple Silicon / ARM64 hosts, " +
            "'linux/amd64' for Intel/AMD x86-64 hosts). " +
            "Infer this from the user's CPU architecture: use 'linux/arm64' for Apple Silicon Macs and ARM64 Linux hosts, " +
            "'linux/amd64' for Intel/AMD hosts. When unsure, ask the user before running.")]
        string platform = "linux/amd64")
    {
        ushort[] ports =
        [
            GlobalSettings.DefaultEventHubAmqpPort,
            GlobalSettings.DefaultServiceBusAmqpPort,
            GlobalSettings.AdditionalServiceBusPort,
            GlobalSettings.DefaultTableStoragePort,
            GlobalSettings.DefaultBlobStoragePort,
            GlobalSettings.DefaultQueueStoragePort,
            GlobalSettings.DefaultFileStoragePort,
            GlobalSettings.DefaultEventHubPort,
            GlobalSettings.DefaultKeyVaultPort,
            GlobalSettings.DefaultResourceManagerPort,
            GlobalSettings.ContainerRegistryPort,
        ];

        var portArgs = string.Join(" ", Array.ConvertAll(ports, p => $"-p {p}:{p}"));

        return $"docker run -d --name {ContainerName} --platform {platform} {portArgs} " +
               $"thecloudtheory/topaz-host:{version} " +
               $"--log-level {logLevel}";
    }

    [McpServerTool]
    [Description(
        "Returns a Docker CLI command that stops and removes the Topaz emulator container. " +
        "Run the returned command in a terminal that has Docker available.")]
    [UsedImplicitly]
    public static string StopTopazContainer()
    {
        return $"docker stop {ContainerName} && docker rm {ContainerName}";
    }
}
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
    private const string NetworkName = "topaz-net";
    private const string NetworkSubnet = "172.28.0.0/16";
    private const string TopazContainerIp = "172.28.0.10";
    private const string DnsContainerName = "topaz-dns";
    private const string DnsContainerIp = "172.28.0.53";

    [McpServerTool]
    [Description(
        "Returns a shell command that: (1) creates the shared topaz-net Docker network with a fixed subnet, " +
        "(2) starts a lightweight dnsmasq DNS resolver that resolves all *.topaz.local.dev subdomains " +
        "(Key Vault, Storage, Service Bus, Event Hub data-plane hostnames) to the Topaz container, " +
        "(3) starts the Topaz emulator container at a fixed IP on that network. " +
        "Run the returned command in a terminal that has Docker available. " +
        "IMPORTANT: start the MCP container with --network topaz-net --dns 172.28.0.53 so all service subdomains resolve correctly: " +
        "docker run -i --network topaz-net --dns 172.28.0.53 topaz/mcp")]
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

        var dnsCommand = $"docker run -d --name {DnsContainerName} --network {NetworkName} --ip {DnsContainerIp} " +
                         $"alpine sh -c \"apk add -q --no-cache dnsmasq && " +
                         $"dnsmasq --no-daemon --no-resolv --server=8.8.8.8 --address=/.topaz.local.dev/{TopazContainerIp}\"";

        var topazCommand = $"docker run -d --name {ContainerName} --network {NetworkName} --ip {TopazContainerIp} " +
                           $"--platform {platform} {portArgs} " +
                           $"thecloudtheory/topaz-host:{version} " +
                           $"--log-level {logLevel}";

        return $"docker network create --subnet {NetworkSubnet} {NetworkName} 2>/dev/null; " +
               $"{dnsCommand}; " +
               $"{topazCommand}";
    }

    [McpServerTool]
    [Description(
        "Returns a shell command that connects a running MCP container to the topaz-net Docker network. " +
        "NOTE: for full wildcard subdomain DNS support (Key Vault, Storage, Service Bus, Event Hub data-plane endpoints), " +
        "the MCP container must be started with --dns 172.28.0.53. Because Docker DNS settings are fixed at container creation time, " +
        "connecting an already-running container to the network only restores base ARM connectivity (topaz.local.dev). " +
        "For full support, restart the MCP container with: docker run -i --network topaz-net --dns 172.28.0.53 topaz/mcp. " +
        "Run the returned command in a terminal that has Docker available.")]
    [UsedImplicitly]
    public static string ConnectMcpToTopazNetwork(
        [Description("Name or ID of the running MCP container to connect to the topaz-net network.")]
        string mcpContainerNameOrId)
    {
        return $"docker network connect {NetworkName} {mcpContainerNameOrId}";
    }

    [McpServerTool]
    [Description(
        "Returns a shell command that stops and removes the Topaz emulator container, the DNS resolver container, " +
        "and the shared topaz-net Docker network. " +
        "Run the returned command in a terminal that has Docker available.")]
    [UsedImplicitly]
    public static string StopTopazContainer()
    {
        return $"docker stop {ContainerName} {DnsContainerName} && " +
               $"docker rm {ContainerName} {DnsContainerName} && " +
               $"docker network rm {NetworkName}";
    }
}
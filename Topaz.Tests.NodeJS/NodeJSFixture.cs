using System.Text;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

namespace Topaz.Tests.NodeJS;

[SetUpFixture]
public class NodeJSFixture
{
    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey = File.ReadAllText("topaz.key");

    private IFutureDockerImage? _nodeImage;
    private IContainer? _containerTopaz;
    private INetwork? _network;

    internal static IContainer? NodeContainer { get; private set; }
    internal static IContainer? TopazContainer { get; private set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var repoRoot = FindRepoRoot();

        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        _containerTopaz = new ContainerBuilder()
            .WithImage(TopazContainerImage)
            .WithPortBinding(8888)
            .WithPortBinding(8889)
            .WithPortBinding(8891)
            .WithPortBinding(8897)
            .WithPortBinding(8898)
            .WithPortBinding(8899)
            .WithNetwork(_network)
            .WithName("topaz.local.dev")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/app/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateKey), "/app/topaz.key")
            .WithCommand(
                "--certificate-file", "topaz.crt",
                "--certificate-key", "topaz.key",
                "--log-level",
                Environment.GetEnvironmentVariable("CI") == "true" ? "Information" : "Debug",
                "--default-subscription", Guid.NewGuid().ToString())
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await _containerTopaz.StartAsync().ConfigureAwait(false);

        await WaitForContainerReady(_containerTopaz, 8899).ConfigureAwait(false);

        _nodeImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(repoRoot)
            .WithDockerfile("Topaz.Tests.NodeJS/docker/Dockerfile")
            .WithBuildArgument("BUILDKIT_INLINE_CACHE", "1")
            .Build();

        await _nodeImage.CreateAsync().ConfigureAwait(false);

        var topazIp = _containerTopaz.IpAddress;

        var nodeContainer = new ContainerBuilder()
            .WithImage(_nodeImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithEnvironment("NODE_EXTRA_CA_CERTS", "/tmp/topaz.crt")
            .WithExtraHost("topaz.local.dev", topazIp)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await nodeContainer.StartAsync().ConfigureAwait(false);

        NodeContainer = nodeContainer;
        TopazContainer = _containerTopaz;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (NodeContainer != null) await NodeContainer.DisposeAsync();
        if (_containerTopaz != null) await _containerTopaz.DisposeAsync();
        if (_nodeImage != null) await _nodeImage.DisposeAsync();
        if (_network != null) await _network.DisposeAsync();
    }

    /// <summary>
    /// Runs a Node.js smoke script inside the Node container and asserts exit code 0.
    /// </summary>
    internal static async Task RunNodeScript(string scriptFile)
    {
        Assert.That(NodeContainer, Is.Not.Null, "Node container has not been started.");
        Assert.That(TopazContainer, Is.Not.Null, "Topaz container has not been started.");

        var result = await NodeContainer!.ExecAsync([
            "/bin/sh", "-c",
            $"cd /tests && node {scriptFile} 2>&1"
        ]);

        Console.WriteLine($"node {scriptFile}:");
        Console.WriteLine(result.Stdout);

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync(result.Stderr);
        }

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"node {scriptFile} failed.\n\n{result.Stdout}\n{result.Stderr}");
    }

    /// <summary>
    /// Ensures a hostname resolves to the Topaz container IP inside the Node container.
    /// </summary>
    internal static async Task EnsureHostMapping(string hostname)
    {
        Assert.That(NodeContainer, Is.Not.Null);
        Assert.That(TopazContainer, Is.Not.Null);

        var result = await NodeContainer!.ExecAsync([
            "/bin/sh", "-c",
            $"grep -qiE '(^|[[:space:]]){Regex.Escape(hostname)}([[:space:]]|$)' /etc/hosts " +
            $"|| echo '{TopazContainer!.IpAddress} {hostname}' >> /etc/hosts"
        ]);

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"Failed to register host mapping for {hostname}. STDERR: {result.Stderr}");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Topaz.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find repo root (Topaz.sln).");
    }

    private static async Task WaitForContainerReady(IContainer container, int port)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient();
                await tcp.ConnectAsync("127.0.0.1", container.GetMappedPublicPort(port));
                return;
            }
            catch
            {
                await Task.Delay(500);
            }
        }
        throw new TimeoutException($"Topaz container port {port} did not become ready within 60 s.");
    }
}

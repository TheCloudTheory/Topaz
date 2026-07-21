using System.Text;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Topaz.Tests.Legacy.Java;

[SetUpFixture]
public class JavaFixture
{
    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

    private static readonly string JavaTestImage =
        Environment.GetEnvironmentVariable("TOPAZ_JAVA_TEST_IMAGE") ?? "topaz-java-legacy-test";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey = File.ReadAllText("topaz.key");

    private IContainer? _containerTopaz;
    private INetwork? _network;

    private static IContainer? JavaContainer { get; set; }
    private static IContainer? TopazContainer { get; set; }

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        var subscriptionId = Guid.NewGuid().ToString();

        _containerTopaz = new ContainerBuilder()
            .WithImage(TopazContainerImage)
            .WithPortBinding(8888)
            .WithPortBinding(8891)
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
                "--default-subscription", subscriptionId)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await _containerTopaz.StartAsync().ConfigureAwait(false);

        await WaitForContainerReady(_containerTopaz, 8899).ConfigureAwait(false);

        var topazIp = _containerTopaz.IpAddress;

        var javaContainer = new ContainerBuilder()
            .WithImage(JavaTestImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithExtraHost("topaz.local.dev", topazIp)
            .WithEnvironment("TOPAZ_SUBSCRIPTION_ID", subscriptionId)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await javaContainer.StartAsync().ConfigureAwait(false);

        // Import the Topaz certificate into the JVM trust store
        var importCert = await javaContainer.ExecAsync([
            "/bin/sh", "-c",
            "keytool -import -noprompt -trustcacerts -alias topaz " +
            "-file /tmp/topaz.crt -keystore $JAVA_HOME/lib/security/cacerts -storepass changeit"
        ]);

        Assert.That(importCert.ExitCode, Is.EqualTo(0),
            $"Failed to import Topaz certificate into JVM trust store. STDERR: {importCert.Stderr}");

        JavaContainer = javaContainer;
        TopazContainer = _containerTopaz;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (JavaContainer != null) await JavaContainer.DisposeAsync();
        if (_containerTopaz != null) await _containerTopaz.DisposeAsync();
        if (_network != null) await _network.DisposeAsync();
    }

    /// <summary>
    /// Runs a Maven test class inside the Java container and asserts exit code 0.
    /// </summary>
    internal static async Task RunJavaTests(string testClass)
    {
        Assert.Multiple(() =>
        {
            Assert.That(JavaContainer, Is.Not.Null, "Java container has not been started.");
            Assert.That(TopazContainer, Is.Not.Null, "Topaz container has not been started.");
        });

        var result = await JavaContainer!.ExecAsync([
            "/bin/sh", "-c",
            $"cd /tests && mvn -q test -Dtest={testClass} -pl . 2>&1"
        ]);

        Console.WriteLine($"mvn test {testClass}:");
        Console.WriteLine(result.Stdout);

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync(result.Stderr);
        }

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"Java test {testClass} failed.\n\n{result.Stdout}\n{result.Stderr}");
    }

    /// <summary>
    /// Adds an /etc/hosts entry inside the Java container so the test can
    /// reach Topaz storage subdomains.
    /// </summary>
    internal static async Task EnsureHostMapping(string hostname)
    {
        Assert.Multiple(() =>
        {
            Assert.That(JavaContainer, Is.Not.Null);
            Assert.That(TopazContainer, Is.Not.Null);
        });

        var result = await JavaContainer!.ExecAsync([
            "/bin/sh", "-c",
            $"grep -qiE '(^|[[:space:]]){Regex.Escape(hostname)}([[:space:]]|$)' /etc/hosts " +
            $"|| echo '{TopazContainer!.IpAddress} {hostname}' >> /etc/hosts"
        ]);

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"Failed to register host mapping for {hostname}. STDERR: {result.Stderr}");
    }

    private static async Task WaitForContainerReady(IContainer container, ushort containerPort, int timeoutSeconds = 60)
    {
        var hostPort = container.GetMappedPublicPort(containerPort);
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient();
                await tcp.ConnectAsync("localhost", hostPort).ConfigureAwait(false);
                return;
            }
            catch
            {
                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        throw new TimeoutException($"Topaz container port {containerPort} did not become ready within {timeoutSeconds}s.");
    }
}

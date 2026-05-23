using System.Text;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

namespace Topaz.Tests.Python;

[SetUpFixture]
public class PythonFixture
{
    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey = File.ReadAllText("topaz.key");

    private IFutureDockerImage? _pythonImage;
    private IContainer? _containerTopaz;
    private INetwork? _network;

    internal static IContainer? PythonContainer { get; private set; }
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
            .WithPortBinding(8890)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8897)
            .WithPortBinding(8891)
            .WithPortBinding(8893)
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
        await Task.Delay(TimeSpan.FromSeconds(3));

        _pythonImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(repoRoot)
            .WithDockerfile("Topaz.Tests.Python/docker/Dockerfile")
            .WithBuildArgument("BUILDKIT_INLINE_CACHE", "1")
            .Build();

        await _pythonImage.CreateAsync().ConfigureAwait(false);

        var pythonContainer = new ContainerBuilder()
            .WithImage(_pythonImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithEnvironment("REQUESTS_CA_BUNDLE", "/tmp/topaz.crt")
            .WithEnvironment("SSL_CERT_FILE", "/etc/ssl/certs/ca-certificates.crt")
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            .WithEnvironment("AZURE_KEYVAULT_DISABLE_CHALLENGE_RESOURCE_VERIFICATION", "true")
            .WithExtraHost("topaz.local.dev", _containerTopaz.IpAddress)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await pythonContainer.StartAsync().ConfigureAwait(false);

        var appendCertResult = await pythonContainer.ExecAsync([
            "/bin/sh", "-c",
            "cat /tmp/topaz.crt >> /etc/ssl/certs/ca-certificates.crt"
        ]);

        Assert.That(appendCertResult.ExitCode, Is.EqualTo(0),
            $"Appending Topaz certificate to system trust store failed. STDERR: {appendCertResult.Stderr}");

        // Also update certifi's CA bundle so that uAMQP / pyamqp trusts the Topaz cert
        await pythonContainer.ExecAsync([
            "/bin/sh", "-c",
            "python3 -c \"import certifi; open(certifi.where(), 'a').write(open('/tmp/topaz.crt').read())\" 2>/dev/null || true"
        ]);

        PythonContainer = pythonContainer;
        TopazContainer = _containerTopaz;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (PythonContainer != null) await PythonContainer.DisposeAsync();
        if (_containerTopaz != null) await _containerTopaz.DisposeAsync();
        if (_pythonImage != null) await _pythonImage.DisposeAsync();
        if (_network != null) await _network.DisposeAsync();
    }

    /// <summary>
    /// Runs pytest against the specified test file inside the Python container and
    /// asserts that the exit code is 0.  On failure the full pytest output is
    /// included in the assertion message.
    /// </summary>
    internal static async Task RunPythonTests(string testFile)
    {
        Assert.That(PythonContainer, Is.Not.Null, "Python container has not been started.");
        Assert.That(TopazContainer, Is.Not.Null, "Topaz container has not been started.");

        var result = await PythonContainer!.ExecAsync([
            "/bin/sh", "-c",
            $"cd /tests && python -m pytest {testFile} -v --tb=short 2>&1"
        ]);

        Console.WriteLine($"pytest {testFile}:");
        Console.WriteLine(result.Stdout);

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync(result.Stderr);
        }

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"pytest {testFile} failed.\n\n{result.Stdout}\n{result.Stderr}");
    }

    /// <summary>
    /// Ensures a hostname is resolvable to the Topaz container IP inside the
    /// Python container, adding an /etc/hosts entry if not already present.
    /// </summary>
    internal static async Task EnsureHostMapping(string hostname)
    {
        Assert.That(PythonContainer, Is.Not.Null);
        Assert.That(TopazContainer, Is.Not.Null);

        var result = await PythonContainer!.ExecAsync([
            "/bin/sh", "-c",
            $"grep -qiE '(^|[[:space:]]){Regex.Escape(hostname)}([[:space:]]|$)' /etc/hosts " +
            $"|| echo '{TopazContainer!.IpAddress} {hostname}' >> /etc/hosts"
        ]);

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"Failed to register host mapping for {hostname}. STDERR: {result.Stderr}");
    }

    // ------------------------------------------------------------------

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Topaz.sln")))
            directory = directory.Parent;

        return directory?.FullName
               ?? throw new InvalidOperationException(
                   "Repository root (directory containing Topaz.sln) could not be located.");
    }
}

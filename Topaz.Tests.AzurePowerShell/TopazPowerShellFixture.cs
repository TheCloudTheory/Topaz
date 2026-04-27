using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Topaz.Tests.AzurePowerShell;

/// <summary>
/// Namespace-level setup fixture: starts shared Topaz + PowerShell containers once
/// for all test classes in this project and tears them down when the run finishes.
/// </summary>
[SetUpFixture]
public class TopazPowerShellFixture
{
    private const string PowerShellContainerImage = "mcr.microsoft.com/powershell:lts";

    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey  = File.ReadAllText("topaz.key");

    private static readonly string SetupScriptContent =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "setup-az-environment.ps1"));

    // Shared across all test classes via PowerShellTestBase
    internal static IContainer? ContainerPowerShell;
    internal static IContainer? ContainerTopaz;

    private static INetwork? _network;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        ContainerTopaz = new ContainerBuilder()
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
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateKey),  "/app/topaz.key")
            .WithCommand("--certificate-file", "topaz.crt", "--certificate-key",
                "topaz.key", "--log-level", "Debug", "--default-subscription",
                Guid.NewGuid().ToString())
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await ContainerTopaz.StartAsync().ConfigureAwait(false);

        // Give Topaz a moment to initialise
        await Task.Delay(TimeSpan.FromSeconds(3));

        ContainerPowerShell = new ContainerBuilder()
            .WithImage(PowerShellContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(SetupScriptContent), "/tmp/setup-az-environment.ps1")
            // After update-ca-certificates runs, .NET will use this bundle
            .WithEnvironment("SSL_CERT_FILE", "/etc/ssl/certs/ca-certificates.crt")
            // Disable real AAD instance discovery for all child processes
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            .Build();

        await ContainerPowerShell.StartAsync().ConfigureAwait(false);

        // Trust the Topaz self-signed certificate inside the Ubuntu-based container.
        // ca-certificates is typically pre-installed; update-ca-certificates reads from
        // /usr/local/share/ca-certificates/ and regenerates /etc/ssl/certs/ca-certificates.crt.
        var trustResult = await ContainerPowerShell.ExecAsync(new List<string>
        {
            "/bin/sh", "-c",
            "cp /tmp/topaz.crt /usr/local/share/ca-certificates/topaz.crt && " +
            "update-ca-certificates"
        });

        Assert.That(trustResult.ExitCode, Is.EqualTo(0),
            $"Failed to trust Topaz certificate. STDOUT: {trustResult.Stdout}, STDERR: {trustResult.Stderr}");

        // Install Az modules, register the Topaz environment, and authenticate.
        // This may take several minutes on first run due to PSGallery downloads.
        var setupResult = await ContainerPowerShell.ExecAsync(new List<string>
        {
            "pwsh", "-NonInteractive", "-File", "/tmp/setup-az-environment.ps1"
        });

        Assert.That(setupResult.ExitCode, Is.EqualTo(0),
            $"Az PowerShell setup failed. STDOUT: {setupResult.Stdout}, STDERR: {setupResult.Stderr}");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        KeyVaultHostMapper.ResetCache();
        if (ContainerPowerShell != null) await ContainerPowerShell.DisposeAsync();
        if (ContainerTopaz != null)      await ContainerTopaz.DisposeAsync();
        if (_network != null)            await _network.DisposeAsync();
    }
}

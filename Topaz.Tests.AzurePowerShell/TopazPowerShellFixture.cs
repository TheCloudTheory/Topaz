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
    // Use the custom-built topaz/powershell image (ubuntu:22.04 + PowerShell 7 installed
    // natively for the current platform via scripts/build-powershell-container.sh).
    // This avoids the arm/v7 limitation of mcr.microsoft.com/powershell:lts and the
    // Az module assembly-load-context failures that occur under x86_64 QEMU emulation.
    private const string PowerShellContainerImage = "topaz/powershell";

    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey  = File.ReadAllText("topaz.key");

    private static readonly string SetupScriptContent =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "setup-az-environment.ps1"));

    private static readonly string RestoreScriptContent =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "restore-az-context.ps1"));

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
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/app/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateKey),  "/app/topaz.key")
            .WithCommand("--certificate-file", "topaz.crt", "--certificate-key",
                "topaz.key", "--log-level", "Debug", "--default-subscription",
                Guid.NewGuid().ToString())
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            // UntilPortIsAvailable probes the container's internal IP directly, which is
            // unreachable from macOS (Docker Desktop runs containers inside a Linux VM).
            // UntilHttpRequestIsSucceeded uses container.Hostname + GetMappedPublicPort,
            // routing through Docker's host port mapping, which is reachable on all OSes.
            // The self-signed certificate is accepted via UsingHttpMessageHandler.
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPath("/health")
                    .ForPort(8899)
                    .UsingTls()
                    .UsingHttpMessageHandler(new System.Net.Http.HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    })))
            .Build();

        await ContainerTopaz.StartAsync().ConfigureAwait(false);

        ContainerPowerShell = new ContainerBuilder()
            .WithImage(PowerShellContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(SetupScriptContent), "/tmp/setup-az-environment.ps1")
            .WithResourceMapping(Encoding.UTF8.GetBytes(RestoreScriptContent), "/tmp/restore-az-context.ps1")
            // After update-ca-certificates runs, .NET will use this bundle
            .WithEnvironment("SSL_CERT_FILE", "/etc/ssl/certs/ca-certificates.crt")
            // Disable real AAD instance discovery for all child processes
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            // Explicit /etc/hosts entry: Docker's embedded DNS can fail to resolve
            // dotted container names under .NET's resolver; WithExtraHost guarantees it.
            .WithExtraHost("topaz.local.dev", ContainerTopaz.IpAddress)
            // MSAL's background instance-discovery thread constructs a URL of the form
            // https://login.microsoftonline.com:<custom-port>/common/discovery/instance
            // (it inherits the port from our custom ActiveDirectoryAuthority, 8899).
            // Routing that hostname to 127.0.0.1 makes the connection fail immediately
            // with ECONNREFUSED instead of waiting through multiple TCP retransmit
            // timeouts (~2 minutes each), which was causing the setup script to take
            // 15+ minutes. The main auth flow (token grant) is unaffected because it
            // talks directly to topaz.local.dev which /etc/hosts maps to Topaz.
            .WithExtraHost("login.microsoftonline.com", "127.0.0.1")
            .WithExtraHost("login.windows.net", "127.0.0.1")
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

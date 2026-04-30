using System.Text;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

// Run all test fixtures concurrently. Each fixture owns its own container pair so
// there are no shared-state conflicts. 4 workers covers the number of test classes.
[assembly: NUnit.Framework.LevelOfParallelism(4)]

namespace Topaz.Tests.AzurePowerShell;

/// <summary>
/// Base class for all AzurePowerShell test classes.
/// Each concrete fixture gets its own Topaz + PowerShell container pair, started in
/// <see cref="FixtureSetUp"/> and torn down in <see cref="FixtureTearDown"/>. Because
/// fixtures own independent containers they can run in parallel without conflicts.
/// </summary>
public abstract class PowerShellTestBase
{
    private const string PowerShellContainerImage = "topaz/powershell";

    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

    private static readonly string CertificateFile =
        File.ReadAllText("topaz.crt");

    private static readonly string CertificateKey =
        File.ReadAllText("topaz.key");

    private static readonly string SetupScriptContent =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "setup-az-environment.ps1"));

    private static readonly string RestoreScriptContent =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scripts", "restore-az-context.ps1"));

    private INetwork? _network;
    private IContainer? _containerTopaz;
    private IContainer? _containerPowerShell;

    [OneTimeSetUp]
    public async Task FixtureSetUp()
    {
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
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/app/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateKey),  "/app/topaz.key")
            .WithCommand("--certificate-file", "topaz.crt", "--certificate-key",
                "topaz.key", "--log-level", "Debug", "--default-subscription",
                Guid.NewGuid().ToString())
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
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

        await _containerTopaz.StartAsync().ConfigureAwait(false);

        _containerPowerShell = new ContainerBuilder()
            .WithImage(PowerShellContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(SetupScriptContent), "/tmp/setup-az-environment.ps1")
            .WithResourceMapping(Encoding.UTF8.GetBytes(RestoreScriptContent), "/tmp/restore-az-context.ps1")
            .WithEnvironment("SSL_CERT_FILE", "/etc/ssl/certs/ca-certificates.crt")
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            .WithExtraHost("topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("login.microsoftonline.com", "127.0.0.1")
            .WithExtraHost("login.windows.net", "127.0.0.1")
            .Build();

        await _containerPowerShell.StartAsync().ConfigureAwait(false);

        var trustResult = await _containerPowerShell.ExecAsync(new List<string>
        {
            "/bin/sh", "-c",
            "cp /tmp/topaz.crt /usr/local/share/ca-certificates/topaz.crt && " +
            "update-ca-certificates"
        });

        Assert.That(trustResult.ExitCode, Is.EqualTo(0),
            $"Failed to trust Topaz certificate. STDOUT: {trustResult.Stdout}, STDERR: {trustResult.Stderr}");

        var setupResult = await _containerPowerShell.ExecAsync(new List<string>
        {
            "pwsh", "-NonInteractive", "-File", "/tmp/setup-az-environment.ps1"
        });

        Assert.That(setupResult.ExitCode, Is.EqualTo(0),
            $"Az PowerShell setup failed. STDOUT: {setupResult.Stdout}, STDERR: {setupResult.Stderr}");
    }

    [OneTimeTearDown]
    public async Task FixtureTearDown()
    {
        if (_containerPowerShell != null)
        {
            var id = _containerPowerShell.Id;
            if (!string.IsNullOrEmpty(id))
                KeyVaultHostMapper.ResetCache(id);
            await _containerPowerShell.DisposeAsync();
        }
        if (_containerTopaz != null)  await _containerTopaz.DisposeAsync();
        if (_network != null)         await _network.DisposeAsync();
    }

    protected async Task RunAzurePowerShellCommand(
        string script,
        Action<JsonNode>? assertion = null,
        int exitCode = 0)
    {
        if (_containerPowerShell != null && _containerTopaz != null)
        {
            await KeyVaultHostMapper.EnsureVaultHostsMapped(_containerPowerShell, _containerTopaz, script);
        }

        var wrappedScript = BuildAuthenticatedScript(script);

        var result = await _containerPowerShell!.ExecAsync(new List<string>
        {
            "pwsh", "-NonInteractive", "-Command", wrappedScript
        });

        Console.WriteLine($"Script: {script}");

        if (result.ExitCode == 0)
        {
            Console.WriteLine($"STDOUT: {result.Stdout}");
        }

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync($"STDERR: {result.Stderr}");
        }

        Assert.That(result.ExitCode, Is.EqualTo(exitCode),
            $"PowerShell script failed. STDOUT: {result.Stdout}, STDERR: {result.Stderr}");

        if (assertion != null)
        {
            // Az cmdlets may emit WARNING:/VERBOSE: lines before the JSON output.
            // Find the first JSON-starting character so the parser is not tripped up
            // by any non-JSON preamble that slips through.
            var stdout = result.Stdout ?? string.Empty;
            var jsonStart = stdout.IndexOfAny(['{', '[']);
            var jsonText = jsonStart >= 0 ? stdout[jsonStart..] : stdout;
            assertion.Invoke(JsonNode.Parse(jsonText)!);
        }
    }

    private static string BuildAuthenticatedScript(string script)
    {
        var escapedScript = script.Replace("'@", "'@@");

        return string.Join('\n',
            "$PSStyle.OutputRendering = 'PlainText'",
            "$ErrorActionPreference     = \"Stop\"",
            "$WarningPreference         = 'SilentlyContinue'",
            "$InformationPreference     = 'SilentlyContinue'",
            "& /tmp/restore-az-context.ps1",
            "if ($null -eq (Get-AzContext)) { throw \"No Azure context after restore script.\" }",
            "$scriptToRun = @'",
            escapedScript,
            "'@",
            "Invoke-Expression $scriptToRun");
    }
}

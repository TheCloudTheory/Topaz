using System.Text;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Topaz.Shared;

namespace Topaz.Tests.AzureCLI;

/// <summary>
/// Test fixture that starts Topaz WITHOUT TOPAZ_CONTAINERIZED=true, so port 443 is not
/// bound. The built-in CONNECT proxy on port 44380 handles MSAL's user-realm discovery
/// pre-flight (which always targets port 443) by remapping the tunnel to port 8899.
/// The Azure CLI container has HTTPS_PROXY set to route through the proxy.
/// </summary>
public class TopazProxyFixture
{
    private static readonly string AzureCliContainerImage =
        Environment.GetEnvironmentVariable("AZURE_CLI_CONTAINER_IMAGE") ?? "mcr.microsoft.com/azure-cli:2.84.0";

    private const string CloudEnvironmentConfiguration = """
{
  "endpoints": {
    "resourceManager": "https://topaz.local.dev:8899",
    "activeDirectory": "https://topaz.local.dev:8899",
    "activeDirectoryResourceId": "https://topaz.local.dev:8899",
    "activeDirectoryGraphResourceId": "https://topaz.local.dev:8899",
    "microsoft_graph_resource_id": "https://topaz.local.dev:8899",
    "acr_login_server_endpoint": "https://topaz.local.dev:8899"
  },
  "suffixes": {
    "keyvault_dns": ".vault.topaz.local.dev",
    "acrLoginServerEndpoint": ".cr.topaz.local.dev"
  }
}
""";

    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey = File.ReadAllText("topaz.key");

    private IContainer? _containerTopaz;
    private INetwork? _network;
    private IContainer? _containerAzureCli;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        _containerTopaz = new ContainerBuilder()
            .WithImage(TopazContainerImage)
            .WithPortBinding(8899)
            .WithPortBinding(8898)
            .WithPortBinding(8891)
            .WithPortBinding(8893)
            .WithPortBinding(GlobalSettings.ConnectProxyPort)
            // No TOPAZ_CONTAINERIZED=true — port 443 will not be bound; the CONNECT proxy handles remapping.
            .WithNetwork(_network)
            .WithName("topaz.local.dev")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/app/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateKey), "/app/topaz.key")
            .WithCommand("--certificate-file", "topaz.crt", "--certificate-key",
                "topaz.key", "--log-level",
                Environment.GetEnvironmentVariable("CI") == "true" ? "Information" : "Debug",
                "--default-subscription", Guid.NewGuid().ToString(),
                "--emulator-ip-address", "0.0.0.0")
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await _containerTopaz.StartAsync().ConfigureAwait(false);

        await Task.Delay(TimeSpan.FromSeconds(3));

        _containerAzureCli = new ContainerBuilder()
            .WithImage(AzureCliContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CloudEnvironmentConfiguration), "cloud.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            // Route MSAL's port-443 user-realm discovery through the Topaz CONNECT proxy.
            .WithEnvironment("HTTPS_PROXY", $"http://topaz.local.dev:{GlobalSettings.ConnectProxyPort}")
            .WithEnvironment("REQUESTS_CA_BUNDLE",
                "/usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem")
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            .WithExtraHost("topaz.local.dev", _containerTopaz.IpAddress)
            .Build();

        await _containerAzureCli.StartAsync();

        var appendCertResult = await _containerAzureCli.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            "cat /tmp/topaz.crt >> /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem"
        });

        Assert.That(appendCertResult.ExitCode, Is.EqualTo(0),
            $"Appending a self-signed certificate failed. STDOUT: {appendCertResult.Stdout}, STDERR: {appendCertResult.Stderr}");

        await RunAzureCliCommand("az cloud register -n TopazProxy --cloud-config @\"cloud.json\"");
        await RunAzureCliCommand("az cloud set -n TopazProxy");
        await RunAzureCliCommand("az login --username topazadmin@topaz.local.dev --password admin");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _containerTopaz!.DisposeAsync();
        await _containerAzureCli!.DisposeAsync();
        await _network!.DisposeAsync();
    }

    protected async Task RunAzureCliCommand(string command, Action<JsonNode>? assertion = null, int exitCode = 0)
    {
        var result = await _containerAzureCli!.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            command
        });

        Console.WriteLine($"Command: {command}");

        if (result.ExitCode == 0)
        {
            Console.WriteLine($"Command STDOUT: {result.Stdout}");
        }

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync($"Command STDERR: {result.Stderr}");
        }

        Assert.That(result.ExitCode, Is.EqualTo(exitCode),
            $"`{command}` command failed. STDOUT: {result.Stdout}, STDERR: {result.Stderr}");

        assertion?.Invoke(JsonNode.Parse(result.Stdout)!);
    }
}

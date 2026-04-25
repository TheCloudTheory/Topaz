using System.Text;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Topaz.Tests.AzureCLI;

public class TopazFixture
{
    private const string AzureCliContainerImage = "mcr.microsoft.com/azure-cli:2.84.0";

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
    
    private static readonly string TopazContainerImage = Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";
    
    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey = File.ReadAllText("topaz.key");
    
    private IContainer? _containerTopaz;
    private INetwork? _network;
    private IContainer? _containerAzureCli;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var templatesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "templates"));
        if (!Directory.Exists(templatesPath))
        {
            throw new DirectoryNotFoundException(
                $"Templates directory was not found: '{templatesPath}'.");
        }
        
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
            .WithCommand("--certificate-file", "topaz.crt", "--certificate-key",
                "topaz.key", "--log-level", "Debug", "--default-subscription",
                Guid.NewGuid().ToString())
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .Build();

        await _containerTopaz.StartAsync()
            .ConfigureAwait(false);
        
        await Task.Delay(TimeSpan.FromSeconds(3));
        
        _containerAzureCli = new ContainerBuilder()
            .WithImage(AzureCliContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(await File.ReadAllTextAsync(Path.Combine(templatesPath, "empty-deployment.json"))), "/templates/empty-deployment.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(await File.ReadAllTextAsync(Path.Combine(templatesPath, "mi-deployment.json"))), "/templates/mi-deployment.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CloudEnvironmentConfiguration), "cloud.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithEnvironment("REQUESTS_CA_BUNDLE", "/usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem")
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            .WithExtraHost("topazacr06.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrrepolist01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrtaglist01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrheadmanifest01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrheadmanifest02.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrimgdel01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrrepodel01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazstorqueuecrt01.queue.storage.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazstorqueuelist01.queue.storage.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazstorqueuedel01.queue.storage.topaz.local.dev", _containerTopaz.IpAddress)
            .Build();
        
        // Act
        await _containerAzureCli.StartAsync();
        
        var appendCertResult = await _containerAzureCli.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            "cat /tmp/topaz.crt >> /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem"
        });
        
        Assert.That(appendCertResult.ExitCode, Is.EqualTo(0), 
            $"Appending a self-signed certificate failed. STDOUT: {appendCertResult.Stdout}, STDERR: {appendCertResult.Stderr}");

        await RunAzureCliCommand("az cloud register -n Topaz --cloud-config @\"cloud.json\"");
        await RunAzureCliCommand("az cloud set -n Topaz");
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
        if (_containerAzureCli != null && _containerTopaz != null)
        {
            await KeyVaultHostMapper.EnsureVaultHostsMapped(_containerAzureCli, _containerTopaz, command);
            await StorageHostMapper.EnsureStorageHostsMapped(_containerAzureCli, _containerTopaz, command);
        }

        var result = await _containerAzureCli!.ExecAsync(new List<string>()
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

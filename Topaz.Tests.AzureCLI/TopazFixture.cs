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
                                                           "endpoints":{
                                                             "resourceManager": "https://topaz.local.dev:8899",
                                                             "activeDirectory": "https://topaz.local.dev:8899",
                                                             "activeDirectoryResourceId": "https://topaz.local.dev:8899",
                                                             "activeDirectoryGraphResourceId": "https://topaz.local.dev:8899",
                                                             "microsoft_graph_resource_id": "https://topaz.local.dev:8899",
                                                             "acr_login_server_endpoint": "https://topaz.local.dev:8899"
                                                           },
                                                           "suffixes": {
                                                             "keyvault_dns": ".keyvault.topaz.local.dev",
                                                             "acrLoginServerEndpoint": ".cr.topaz.local.dev"
                                                           }
                                                         }
                                                         """;
    
    private static readonly string TopazContainerImage = Environment.GetEnvironmentVariable("TOPAZ_CLI_CONTAINER_IMAGE") == null ? 
        "topaz/cli"
        : Environment.GetEnvironmentVariable("TOPAZ_CLI_CONTAINER_IMAGE")!;
    
    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey = File.ReadAllText("topaz.key");
    private static readonly string TenantId = Environment.GetEnvironmentVariable("TOPAZ_TENANT_ID")!;
    private static readonly string ClientId = Environment.GetEnvironmentVariable("TOPAZ_CLIENT_ID")!;
    private static readonly string ClientSecret = Environment.GetEnvironmentVariable("TOPAZ_CLIENT_SECRET")!;
    
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
            .WithNetwork(_network)
            .WithName("topaz.local.dev")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/app/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateKey), "/app/topaz.key")
            .WithCommand("start", "--tenant-id", TenantId, "--certificate-file", "topaz.crt", "--certificate-key",
                "topaz.key", "--log-level", "Debug", "--default-subscription",
                Guid.NewGuid().ToString())
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
            .WithResourceMapping(Encoding.UTF8.GetBytes(CloudEnvironmentConfiguration), "cloud.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithEnvironment("REQUESTS_CA_BUNDLE", "/usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem")
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            .WithExtraHost("purgevault123.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("deletedvault123.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("deletedvault456.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("recovervault123.keyvault.topaz.local.dev", _containerTopaz.IpAddress)            
            .WithExtraHost("secretlistvault01.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("secretlistvault02.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("recovervault01.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("recovervault02.keyvault.topaz.local.dev", _containerTopaz.IpAddress)            
            .WithExtraHost("SecretListVault02.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacr06.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrrepolist01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrtaglist01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrheadmanifest01.cr.topaz.local.dev", _containerTopaz.IpAddress)
            .WithExtraHost("topazacrheadmanifest02.cr.topaz.local.dev", _containerTopaz.IpAddress)
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
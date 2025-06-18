using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Topaz.Tests.AzureCLI;

public class CloudEnvironmentSetupTests
{
    private const string AzureCliContainerImage = "mcr.microsoft.com/azure-cli:2.62.0-cbl-mariner2.0";
    private const string CloudEnvironmentConfiguration = """
                                                         {
                                                           "endpoints":{
                                                             "resourceManager": "https://topaz:8899",
                                                             "activeDirectoryGraphResourceId": "https://topaz:8899/"
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
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
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
            .WithNetwork(_network)
            .WithName("topaz")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/app/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateKey), "/app/topaz.key")
            .WithCommand("start", "--tenant-id", TenantId, "--certificate-file", "topaz.crt", "--certificate-key", "topaz.key")
            .Build();

        await _containerTopaz.StartAsync()
            .ConfigureAwait(false);
        
        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _containerTopaz!.DisposeAsync();
        await _network!.DisposeAsync();
    }

    [Test]
    public async Task CloudEnvironmentSetupTests_WhenCloudEnvironmentIsRequested_ItShouldBeAvailable()
    {
        // Arrange
        var container = new ContainerBuilder()
            .WithImage(AzureCliContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CloudEnvironmentConfiguration), "cloud.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithEnvironment("REQUESTS_CA_BUNDLE", "/usr/lib64/az/lib/python3.9/site-packages/certifi/cacert.pem")
            .Build();

        try
        {
            // Act
            await container.StartAsync();
            
            var appendCertResult = await container.ExecAsync(new List<string>
            {
                "/bin/sh",
                "-c",
                "cat /tmp/topaz.crt >> /usr/lib64/az/lib/python3.9/site-packages/certifi/cacert.pem"
            });
            
            Assert.That(appendCertResult.ExitCode, Is.EqualTo(0), 
                $"Appending a self-signed certificate failed. STDOUT: {appendCertResult.Stdout}, STDERR: {appendCertResult.Stderr}");
            
            var registerResult = await container.ExecAsync(new List<string>()
            {
                "/bin/sh",
                "-c",
                "az cloud register -n Topaz --cloud-config @\"cloud.json\""
            });
            
            Assert.That(registerResult.ExitCode, Is.EqualTo(0), 
                $"`az cloud register` command failed. STDOUT: {registerResult.Stdout}, STDERR: {registerResult.Stderr}");
            
            var showResult = await container.ExecAsync(new List<string>()
            {
                "/bin/sh",
                "-c",
                "az cloud set -n Topaz"
            });
            
            Assert.That(showResult.ExitCode, Is.EqualTo(0), 
                $"`az cloud show` command failed. STDOUT: {showResult.Stdout}, STDERR: {showResult.Stderr}");
            
            var loginResult = await container.ExecAsync(new List<string>()
            {
                "/bin/sh",
                "-c",
                $"az login --service-principal --username {ClientId} --password {ClientSecret} --tenant {TenantId} --allow-no-subscriptions"
            });
            
            Assert.That(loginResult.ExitCode, Is.EqualTo(0), 
                $"`az login` command failed. STDOUT: {loginResult.Stdout}, STDERR: {loginResult.Stderr}");
        }
        finally
        {
            // Assert
            await container.DisposeAsync();
        }
    }
}
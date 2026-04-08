using System.Text;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Topaz.Service.Entra;

namespace Topaz.Tests.Terraform;

public class TopazFixture
{
    // https://mcr.microsoft.com/v2/azure-cli/tags/list
    private const string AzureCliContainerImage = "mcr.microsoft.com/azure-cli:2.84.0";
    private const string TerraformVersion = "1.10.5";
    private const string CloudConfig = """
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

    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_CLI_CONTAINER_IMAGE") ?? "topaz/cli";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey  = File.ReadAllText("topaz.key");

    private static readonly string TenantId = EntraService.TenantId;

    private string _subscriptionId = string.Empty;

    private IContainer? _containerTopaz;
    private INetwork?   _network;
    private IContainer? _containerTerraform;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _subscriptionId = Guid.NewGuid().ToString();

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
            .WithCommand(
                "start",
                "--tenant-id", TenantId,
                "--certificate-file", "topaz.crt",
                "--certificate-key", "topaz.key",
                "--log-level", "Debug",
                "--default-subscription", _subscriptionId)
            .Build();

        await _containerTopaz.StartAsync().ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(3));

        _containerTerraform = new ContainerBuilder()
            .WithImage(AzureCliContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CloudConfig), "cloud.json")
            // ARM provider environment
            .WithEnvironment("ARM_ENVIRONMENT", "custom")
            .WithEnvironment("ARM_METADATA_HOST", "topaz.local.dev:8899")
            .WithEnvironment("ARM_SUBSCRIPTION_ID", _subscriptionId)
            .WithEnvironment("ARM_TENANT_ID", TenantId)
            .WithEnvironment("ARM_SKIP_PROVIDER_REGISTRATION", "true")
            // azapi custom environment endpoints (required when ARM_ENVIRONMENT=custom)
            .WithEnvironment("ARM_ACTIVE_DIRECTORY_AUTHORITY_HOST", "https://topaz.local.dev:8899/")
            .WithEnvironment("ARM_RESOURCE_MANAGER_ENDPOINT", "https://topaz.local.dev:8899/")
            .WithEnvironment("ARM_RESOURCE_MANAGER_AUDIENCE", "https://management.azure.com/")
            // azuread provider environment
            .WithEnvironment("AZURE_ENVIRONMENT", "custom")
            .WithEnvironment("AZURE_METADATA_HOST", "topaz.local.dev:8899")
            .WithEnvironment("AZURE_TENANT_ID", TenantId)
            // Point azuread's Graph API calls at Topaz
            .WithEnvironment("ARM_MICROSOFT_GRAPH_ENDPOINT", "https://topaz.local.dev:8899")
            // Azure CLI — trust Topaz's self-signed cert (Python/requests)
            .WithEnvironment("REQUESTS_CA_BUNDLE", "/usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem")
            // Disable MSAL instance discovery so az CLI doesn't call login.microsoftonline.com
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            // Share provider binaries across test runs to avoid repeated downloads
            .WithEnvironment("TF_PLUGIN_CACHE_DIR", "/tf-plugin-cache")
            // Suppress upgrade checks and ANSI colour codes
            .WithEnvironment("CHECKPOINT_DISABLE", "1")
            .WithEnvironment("TF_CLI_ARGS", "-no-color")
            .WithExtraHost("topaz.local.dev", _containerTopaz.IpAddress)
            .Build();

        await _containerTerraform.StartAsync().ConfigureAwait(false);

        var setupResult = await _containerTerraform.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            "mkdir -p /tf-plugin-cache /workspace && " +
            // registry.terraform.io has AAAA records; Docker's custom bridge is IPv4-only so Go's
            // HTTP client hangs on the IPv6 attempt until the whole request times out.
            // Pre-resolve to IPv4 and pin it in /etc/hosts so terraform never tries IPv6.
            "(python3 -c \"import socket; ip=socket.getaddrinfo('registry.terraform.io',443,socket.AF_INET)[0][4][0]; open('/etc/hosts','a').write(ip+' registry.terraform.io\\n')\" || true) && " +
            // Append Topaz cert to az CLI's CA bundle (Python/requests)
            "cat /tmp/topaz.crt >> /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem && " +
            // Build combined CA bundle for Go-based Terraform provider binaries (SSL_CERT_FILE).
            // certifi/cacert.pem already contains all public CAs + Topaz's cert (appended above).
            "cp /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem /tmp/combined.pem && " +
            // Install Terraform binary
            $"curl -sSfL https://releases.hashicorp.com/terraform/{TerraformVersion}/terraform_{TerraformVersion}_linux_amd64.zip -o /tmp/terraform.zip && " +
            "python3 -c \"import zipfile; zipfile.ZipFile('/tmp/terraform.zip').extract('terraform', '/usr/local/bin')\" && " +
            "chmod +x /usr/local/bin/terraform && " +
            "rm /tmp/terraform.zip"
        });

        Assert.That(setupResult.ExitCode, Is.EqualTo(0),
            $"Container setup failed. STDOUT: {setupResult.Stdout}, STDERR: {setupResult.Stderr}");

        await RunTerraformContainerCommand("az cloud register -n Topaz --cloud-config @\"cloud.json\"");
        await RunTerraformContainerCommand("az cloud set -n Topaz");
        await RunTerraformContainerCommand("az login --username topazadmin@topaz.local.dev --password admin");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _containerTopaz!.DisposeAsync();
        await _containerTerraform!.DisposeAsync();
        await _network!.DisposeAsync();
    }

    // -------------------------------------------------------------------------
    // Provider-specific run helpers
    // Each scenario name maps to terraform/{provider}/{scenario}/ on disk.
    // -------------------------------------------------------------------------

    protected Task RunTerraformWithAzureRm(string scenario, Action<JsonNode>? assertOutputs = null)
        => RunTerraform("providers/azurerm.tf", $"azurerm/{scenario}", assertOutputs);

    protected Task RunTerraformWithAzureApi(string scenario, Action<JsonNode>? assertOutputs = null)
        => RunTerraform("providers/azapi.tf", $"azapi/{scenario}", assertOutputs);

    protected Task RunTerraformWithAzureAd(string scenario, Action<JsonNode>? assertOutputs = null)
        => RunTerraform("providers/azuread.tf", $"entra/{scenario}", assertOutputs);

    // -------------------------------------------------------------------------
    // Core Terraform lifecycle runner: init → apply → (assert) → destroy
    // -------------------------------------------------------------------------

    private async Task RunTerraform(string providerRelPath, string scenarioRelPath, Action<JsonNode>? assertOutputs)
    {
        var workDir = $"/workspace/{Guid.NewGuid():N}";
        var terraformDir = Path.Combine(AppContext.BaseDirectory, "terraform");

        await ExecTerraform($"mkdir -p {workDir}");

        // Copy provider config (.tf files are combined in the workspace directory by Terraform)
        await WriteFileToContainer(workDir, "provider.tf",
            await File.ReadAllTextAsync(Path.Combine(terraformDir, providerRelPath)));

        // Copy all scenario .tf files
        foreach (var tfFile in Directory.GetFiles(Path.Combine(terraformDir, scenarioRelPath), "*.tf"))
            await WriteFileToContainer(workDir, Path.GetFileName(tfFile),
                await File.ReadAllTextAsync(tfFile));

        await ExecTerraform($"terraform -chdir={workDir} init");
        await ExecTerraform($"terraform -chdir={workDir} apply -auto-approve");

        if (assertOutputs != null)
        {
            var (stdout, _) = await ExecTerraformWithOutput($"terraform -chdir={workDir} output -json");
            assertOutputs(JsonNode.Parse(stdout)!);
        }

        await ExecTerraform($"terraform -chdir={workDir} destroy -auto-approve");
    }

    private async Task WriteFileToContainer(string workDir, string fileName, string content)
    {
        // base64-encode to avoid shell-escaping issues with quotes / newlines
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        await ExecTerraform($"echo '{base64}' | base64 -d > {workDir}/{fileName}");
    }

    private Task ExecTerraform(string command) =>
        ExecTerraformWithOutput(command).ContinueWith(t => { _ = t.Result; });

    private async Task<(string Stdout, string Stderr)> ExecTerraformWithOutput(string command)
    {
        // SSL_CERT_FILE causes Go's TLS stack (Terraform provider binaries) to trust the combined CA bundle
        var wrappedCommand = $"SSL_CERT_FILE=/tmp/combined.pem {command}";

        var result = await _containerTerraform!.ExecAsync(new List<string>
        {
            "/bin/sh", "-c", wrappedCommand
        });

        Console.WriteLine($"[terraform] {command}");

        if (result.Stdout.Length > 0)
            Console.WriteLine(result.Stdout);

        if (result.ExitCode != 0)
            await Console.Error.WriteLineAsync(result.Stderr);

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"`{command}` failed.\nSTDOUT: {result.Stdout}\nSTDERR: {result.Stderr}");

        return (result.Stdout, result.Stderr);
    }

    private async Task RunTerraformContainerCommand(string command)
    {
        var result = await _containerTerraform!.ExecAsync(new List<string>
        {
            "/bin/sh", "-c", command
        });

        Console.WriteLine($"[az] {command}");

        if (result.ExitCode != 0)
            await Console.Error.WriteLineAsync(result.Stderr);

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"`{command}` failed.\nSTDOUT: {result.Stdout}\nSTDERR: {result.Stderr}");
    }
}

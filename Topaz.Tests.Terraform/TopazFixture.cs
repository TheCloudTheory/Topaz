using System.Text;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Models;
using Topaz.Identity;
using Topaz.Service.Entra;

namespace Topaz.Tests.Terraform;

public class TopazFixture
{
    // https://hub.docker.com/r/hashicorp/terraform/tags
    private const string TerraformContainerImage = "hashicorp/terraform:1.10";

    private static readonly string TopazContainerImage =
        Environment.GetEnvironmentVariable("TOPAZ_CLI_CONTAINER_IMAGE") ?? "topaz/cli";

    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");
    private static readonly string CertificateKey  = File.ReadAllText("topaz.key");

    private static readonly string TenantId = EntraService.TenantId;
    private string _clientId     = string.Empty;
    private string _clientSecret = string.Empty;

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

        await CreateServicePrincipalCredentials();

        _containerTerraform = new ContainerBuilder()
            .WithImage(TerraformContainerImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            // ARM provider credentials — used by azurerm and azapi
            .WithEnvironment("ARM_ENVIRONMENT", "custom")
            .WithEnvironment("ARM_METADATA_HOST", "topaz.local.dev:8899")
            .WithEnvironment("ARM_SUBSCRIPTION_ID", _subscriptionId)
            .WithEnvironment("ARM_TENANT_ID", TenantId)
            .WithEnvironment("ARM_CLIENT_ID", _clientId)
            .WithEnvironment("ARM_CLIENT_SECRET", _clientSecret)
            .WithEnvironment("ARM_SKIP_PROVIDER_REGISTRATION", "true")
            // azapi custom environment endpoints (required when ARM_ENVIRONMENT=custom)
            .WithEnvironment("ARM_ACTIVE_DIRECTORY_AUTHORITY_HOST", "https://topaz.local.dev:8899/")
            .WithEnvironment("ARM_RESOURCE_MANAGER_ENDPOINT", "https://topaz.local.dev:8899/")
            .WithEnvironment("ARM_RESOURCE_MANAGER_AUDIENCE", "https://management.azure.com/")
            // azuread provider credentials
            .WithEnvironment("AZURE_ENVIRONMENT", "custom")
            .WithEnvironment("AZURE_METADATA_HOST", "topaz.local.dev:8899")
            .WithEnvironment("AZURE_TENANT_ID", TenantId)
            .WithEnvironment("AZURE_CLIENT_ID", _clientId)
            .WithEnvironment("AZURE_CLIENT_SECRET", _clientSecret)
            // Point azuread's Graph API calls at Topaz (port 8899 serves both ARM and Graph endpoints)
            .WithEnvironment("ARM_MICROSOFT_GRAPH_ENDPOINT", "https://topaz.local.dev:8899")
            // Share provider binaries across test runs to avoid repeated downloads
            .WithEnvironment("TF_PLUGIN_CACHE_DIR", "/tf-plugin-cache")
            // Suppress upgrade checks and ANSI colour codes
            .WithEnvironment("CHECKPOINT_DISABLE", "1")
            .WithEnvironment("TF_CLI_ARGS", "-no-color")
            .WithExtraHost("topaz.local.dev", _containerTopaz.IpAddress)
            .Build();

        await _containerTerraform.StartAsync().ConfigureAwait(false);

        // Build a combined CA bundle (system CAs + Topaz self-signed cert) used
        // via SSL_CERT_FILE on every terraform invocation so Go's TLS stack trusts
        // topaz.local.dev without touching the system CA store permanently.
        var setupResult = await _containerTerraform.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            "mkdir -p /tf-plugin-cache /workspace && " +
            "cp /tmp/topaz.crt /tmp/combined.pem && " +
            "(cat /etc/ssl/certs/ca-certificates.crt >> /tmp/combined.pem 2>/dev/null || true)"
        });

        Assert.That(setupResult.ExitCode, Is.EqualTo(0),
            $"Terraform container setup failed. STDOUT: {setupResult.Stdout}, STDERR: {setupResult.Stderr}");
    }

    private async Task CreateServicePrincipalCredentials()
    {
        var port = _containerTopaz!.GetMappedPublicPort(8899);

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var graphClient = new GraphServiceClient(
            new HttpClient(handler),
            new LocalGraphAuthenticationProvider(),
            $"https://localhost:{port}");

        // Create an application
        var app = await graphClient.Applications.PostAsync(new Application
        {
            DisplayName = "topaz-terraform-sp"
        });

        _clientId = app!.AppId!;

        // Create the service principal linked to the application
        await graphClient.ServicePrincipals.PostAsync(new ServicePrincipal
        {
            AppId = _clientId
        });

        // Add a client secret to the application
        var pwd = await graphClient.Applications[app.Id].AddPassword.PostAsync(new AddPasswordPostRequestBody
        {
            PasswordCredential = new PasswordCredential
            {
                DisplayName = "topaz-terraform-sp"
            }
        });

        _clientSecret = pwd!.SecretText!;
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
        // SSL_CERT_FILE causes Go's TLS stack to trust our combined CA bundle
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
}

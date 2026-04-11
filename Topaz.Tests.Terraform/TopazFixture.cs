using System.Text;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Linq;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Topaz.Service.Entra;

namespace Topaz.Tests.Terraform;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Structure",
    "NUnit1032:The field should be Disposed in a method annotated with [OneTimeTearDownAttribute]",
    Justification = "Shared process-wide fixture resources are cleaned up via ProcessExit hook to avoid per-class reinitialization.")]
public class TopazFixture
{
    // https://hub.docker.com/r/hashicorp/terraform/tags
    private const string AzureCliContainerImage = "mcr.microsoft.com/azure-cli:2.84.0";
    private const string TerraformVersion = "1.10.5";

    // Persisted on the host so providers and the Terraform binary are downloaded only once
    // across all test-class fixture setups, regardless of how many containers are created.
    private static readonly string TerraformCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".terraform.d", "topaz-test-cache");
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

    private static readonly SemaphoreSlim SetupLock = new(1, 1);
    private static bool _isInitialized;
    private static bool _cleanupHookRegistered;
    private static string _subscriptionId = string.Empty;

    private static IContainer? _containerTopaz;
    private static INetwork?   _network;
    private static IContainer? _containerTerraform;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await SetupLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isInitialized)
                return;

            _subscriptionId = Guid.NewGuid().ToString();

            Directory.CreateDirectory(TerraformCacheDir);

            RemoveBlockingTopazContainerIfPresent();

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
                // ARM provider identity context
                .WithEnvironment("ARM_SUBSCRIPTION_ID", _subscriptionId)
                .WithEnvironment("AZURE_SUBSCRIPTION_ID", _subscriptionId)
                .WithEnvironment("TF_VAR_subscription_id", _subscriptionId)
                .WithEnvironment("ARM_TENANT_ID", TenantId)
                // azuread provider identity context
                .WithEnvironment("AZURE_TENANT_ID", TenantId)
                // Point azuread's Graph API calls at Topaz
                .WithEnvironment("ARM_MICROSOFT_GRAPH_ENDPOINT", "https://topaz.local.dev:8899")
                // Azure CLI — trust Topaz's self-signed cert (Python/requests)
                .WithEnvironment("REQUESTS_CA_BUNDLE", "/usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem")
                // Disable MSAL instance discovery so az CLI doesn't call login.microsoftonline.com
                .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
                // Share provider binaries across test runs to avoid repeated downloads
                .WithEnvironment("TF_PLUGIN_CACHE_DIR", "/tf-cache/plugin-cache")
                // Suppress upgrade checks and ANSI colour codes
                .WithEnvironment("CHECKPOINT_DISABLE", "1")
                .WithEnvironment("TF_CLI_ARGS", "-no-color")
                // Some CI/container environments start large providers slowly (azurerm schema load).
                .WithEnvironment("TF_PLUGIN_TIMEOUT", "5m")
                .WithExtraHost("topaz.local.dev", _containerTopaz.IpAddress)
                // Bind-mount host cache: providers + terraform binary are downloaded once and reused
                // across all 17 fixture setups rather than re-downloaded for every test class.
                .WithBindMount(TerraformCacheDir, "/tf-cache")
                .Build();

            await _containerTerraform.StartAsync().ConfigureAwait(false);

            var setupResult = await _containerTerraform.ExecAsync(new List<string>
            {
                "/bin/sh",
                "-c",
                "mkdir -p /tf-cache/plugin-cache /tf-cache/binaries /workspace && " +
                // Best-effort entropy feeder for Go-based providers (azurerm v4 can stall on crypto/rand in CI).
                "((command -v rngd >/dev/null 2>&1 && rngd -f -r /dev/urandom -o /dev/random >/dev/null 2>&1 &) || " +
                " (command -v haveged >/dev/null 2>&1 && haveged -F -w 1024 >/dev/null 2>&1 &) || true) && " +
                // Ensure /dev/random reads do not block provider startup in constrained container environments.
                "(ln -sf /dev/urandom /dev/random || true) && " +
                // Append Topaz cert to az CLI's CA bundle (Python/requests)
                "cat /tmp/topaz.crt >> /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem && " +
                // Build combined CA bundle for Go-based Terraform provider binaries (SSL_CERT_FILE).
                // certifi/cacert.pem already contains all public CAs + Topaz's cert (appended above).
                "cp /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem /tmp/combined.pem && " +
                // Install Terraform binary matching container CPU architecture.
                // The cache directory is bind-mounted from the host so it persists across fixture setups.
                "TF_ARCH=$(uname -m | sed 's/x86_64/amd64/; s/aarch64/arm64/; s/arm64/arm64/') && " +
                $"([ -f /tf-cache/binaries/terraform_{TerraformVersion}_${{TF_ARCH}} ] || (" +
                $"curl -sSfL https://releases.hashicorp.com/terraform/{TerraformVersion}/terraform_{TerraformVersion}_linux_${{TF_ARCH}}.zip -o /tmp/terraform.zip && " +
                "python3 -c \"import zipfile; zipfile.ZipFile('/tmp/terraform.zip').extract('terraform', '/tf-cache/binaries')\" && " +
                $"mv /tf-cache/binaries/terraform /tf-cache/binaries/terraform_{TerraformVersion}_${{TF_ARCH}} && " +
                "chmod +x /tf-cache/binaries/terraform_" + TerraformVersion + "_${TF_ARCH} && " +
                "rm /tmp/terraform.zip)) && " +
                $"cp /tf-cache/binaries/terraform_{TerraformVersion}_${{TF_ARCH}} /usr/local/bin/terraform"
            });

            Assert.That(setupResult.ExitCode, Is.EqualTo(0),
                $"Container setup failed. STDOUT: {setupResult.Stdout}, STDERR: {setupResult.Stderr}");

            await WaitForTopazReadiness();

            await RunTerraformContainerCommand("az cloud register -n Topaz --cloud-config @\"cloud.json\"", maxAttempts: 5);
            await RunTerraformContainerCommand("az cloud set -n Topaz", maxAttempts: 3);
            await RunTerraformContainerCommand("az login --username topazadmin@topaz.local.dev --password admin", maxAttempts: 3);
            await EnsureSubscriptionExistsInTopaz();
            await RunTerraformContainerCommand($"az account set --subscription {_subscriptionId}", maxAttempts: 3);

            RegisterCleanupHook();
            _isInitialized = true;
        }
        finally
        {
            SetupLock.Release();
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Shared containers are intentionally reused across all fixture classes.
        // Cleanup is registered once and performed at process exit.
        await Task.CompletedTask;
    }

    private static void RegisterCleanupHook()
    {
        if (_cleanupHookRegistered)
            return;

        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupSharedResources();
        _cleanupHookRegistered = true;
    }

    private static void CleanupSharedResources()
    {
        try
        {
            _containerTopaz?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _containerTerraform?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _network?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            // Best-effort cleanup during process shutdown.
        }
    }

    private static void RemoveBlockingTopazContainerIfPresent()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "rm -f topaz.local.dev",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            process.WaitForExit(10_000);
        }
        catch
        {
            // Best-effort cleanup: if docker is unavailable, setup will fail with a clear error later.
        }
    }

    private static string ReadDockerLogs(string containerName, int tailLines)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"logs --tail {tailLines} {containerName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();
            process.WaitForExit(15_000);

            if (process.ExitCode != 0)
                return string.Empty;

            var interesting = stdout
                .Split('\n')
                .Where(line =>
                    line.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Request", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("endpoint", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                .TakeLast(80);

            return string.Join('\n', interesting);
        }
        catch
        {
            return string.Empty;
        }
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

        await ExecTerraformWithRetry(
            $"terraform -chdir={workDir} init",
            maxAttempts: 3,
            shouldRetry: (_, stderr) =>
                stderr.Contains("Failed to query available provider packages", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("could not connect to registry.terraform.io", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("context deadline exceeded", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("Failed to install provider", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("unexpected EOF", StringComparison.OrdinalIgnoreCase),
            onRetry: async () =>
            {
                await ExecTerraform($"rm -rf {workDir}/.terraform");
                await ExecTerraform("find /tf-cache/plugin-cache -path '*registry.terraform.io/hashicorp/azurerm*' -exec rm -rf {} + || true");
            });

        try
        {
            await ExecTerraform($"terraform -chdir={workDir} apply -auto-approve");
        }
        catch (AssertionException ex) when (IsRecoverableProviderStartupError(ex.Message))
        {
            // The provider cache can occasionally contain a corrupted azurerm artifact.
            // Purge azurerm cache entries, then re-init and retry apply once.
            await ExecTerraform($"rm -rf {workDir}/.terraform");
            await ExecTerraform("find /tf-cache/plugin-cache -path '*registry.terraform.io/hashicorp/azurerm*' -exec rm -rf {} + || true");

            await ExecTerraformWithRetry(
                $"terraform -chdir={workDir} init",
                maxAttempts: 3,
                shouldRetry: (_, stderr) =>
                    stderr.Contains("Failed to query available provider packages", StringComparison.OrdinalIgnoreCase) ||
                    stderr.Contains("could not connect to registry.terraform.io", StringComparison.OrdinalIgnoreCase) ||
                    stderr.Contains("context deadline exceeded", StringComparison.OrdinalIgnoreCase) ||
                    stderr.Contains("Failed to install provider", StringComparison.OrdinalIgnoreCase) ||
                    stderr.Contains("unexpected EOF", StringComparison.OrdinalIgnoreCase),
                onRetry: async () =>
                {
                    await ExecTerraform($"rm -rf {workDir}/.terraform");
                    await ExecTerraform("find /tf-cache/plugin-cache -path '*registry.terraform.io/hashicorp/azurerm*' -exec rm -rf {} + || true");
                });

            await ExecTerraform($"terraform -chdir={workDir} apply -auto-approve");
        }

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

    private async Task ExecTerraformWithRetry(
        string command,
        int maxAttempts,
        Func<string, string, bool> shouldRetry,
        Func<Task>? onRetry = null)
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await ExecTerraformWithOutput(command);
                return;
            }
            catch (AssertionException ex)
            {
                lastError = ex;

                var message = ex.Message;
                if (attempt >= maxAttempts || !shouldRetry(message, message))
                    throw;

                if (onRetry is not null)
                    await onRetry();

                await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
            }
        }

        if (lastError is not null)
            throw lastError;
    }

    private static bool IsRecoverableProviderStartupError(string message)
    {
        return message.Contains("Failed to load plugin schemas", StringComparison.OrdinalIgnoreCase)
            || message.Contains("timeout while waiting for plugin to start", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Unrecognized remote plugin message", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Failed to read any lines from plugin's stdout", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed to instantiate provider", StringComparison.OrdinalIgnoreCase);
    }

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
        {
            await Console.Error.WriteLineAsync(result.Stderr);

            var topazLogs = ReadDockerLogs("topaz.local.dev", 400);
            if (!string.IsNullOrWhiteSpace(topazLogs))
                await Console.Error.WriteLineAsync($"[docker logs topaz.local.dev]\n{topazLogs}");
        }

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"`{command}` failed.\nSTDOUT: {result.Stdout}\nSTDERR: {result.Stderr}");

        return (result.Stdout, result.Stderr);
    }

    private async Task WaitForTopazReadiness()
    {
        for (var attempt = 1; attempt <= 30; attempt++)
        {
            var probe = await _containerTerraform!.ExecAsync(new List<string>
            {
                "/bin/sh",
                "-c",
                "curl -fsS --cacert /tmp/topaz.crt \"https://topaz.local.dev:8899/metadata/endpoints?api-version=2022-09-01\" >/dev/null"
            });

            if (probe.ExitCode == 0)
                return;

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        var topazLogs = ReadDockerLogs("topaz.local.dev", 400);
        Assert.Fail($"Topaz endpoint did not become ready in time. Recent logs:\n{topazLogs}");
    }

    private async Task RunTerraformContainerCommand(string command, int maxAttempts = 1)
    {
        ExecResult result = default;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            result = await _containerTerraform!.ExecAsync(new List<string>
            {
                "/bin/sh", "-c", command
            });

            if (result.ExitCode == 0)
                break;

            if (attempt < maxAttempts)
                await Task.Delay(TimeSpan.FromSeconds(2));
        }

        Console.WriteLine($"[az] {command}");

        if (result.ExitCode != 0)
            await Console.Error.WriteLineAsync(result.Stderr);

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"`{command}` failed.\nSTDOUT: {result.Stdout}\nSTDERR: {result.Stderr}");
    }

    private async Task EnsureSubscriptionExistsInTopaz()
    {
        // Make subscription bootstrap deterministic even if host startup args are ignored.
        var createCommand =
            "status=$(curl -sS -o /tmp/sub-create.out -w '%{http_code}' --cacert /tmp/topaz.crt " +
            $"-X POST \"https://topaz.local.dev:8899/subscriptions/{_subscriptionId}?api-version=2022-12-01\" " +
            "-H \"Content-Type: application/json\" " +
            $"-d '{{\"subscriptionId\":\"{_subscriptionId}\",\"subscriptionName\":\"Topaz - Default\"}}'); " +
            "[ \"$status\" = \"201\" ] || [ \"$status\" = \"400\" ]";

        await RunTerraformContainerCommand(createCommand);

        var verifyCommand =
            $"az rest --method get --url \"https://topaz.local.dev:8899/subscriptions/{_subscriptionId}?api-version=2022-12-01\"";

        await RunTerraformContainerCommand(verifyCommand);
    }
}

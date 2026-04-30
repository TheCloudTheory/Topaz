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

    // Pre-initialized provider workspaces — each provider is initialized once in OneTimeSetUp
    // and then copied (cp -a) into every test workspace, eliminating the 30–90 s per-test
    // `terraform init` cost caused by azurerm v4 schema loading.
    private const string TemplateRoot = "/tf-templates";
    private static readonly string TemplateAzureRm  = $"{TemplateRoot}/azurerm";
    private static readonly string TemplateAzureApi = $"{TemplateRoot}/azapi";
    private static readonly string TemplateAzureAd  = $"{TemplateRoot}/azuread";

    private const string CloudConfig = """
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
                // Enable Terraform debug logging so we can see the exact HTTP requests/responses
                // the provider sends (Authorization header, request path, etc.) on test failure.
                .WithEnvironment("TF_LOG", "DEBUG")
                .WithEnvironment("TF_LOG_PATH", "/tmp/tf-debug.log")
                .WithExtraHost("topaz.local.dev", _containerTopaz.IpAddress)
                // Key Vault data-plane: the azurerm provider pings the vault URI to verify availability.
                .WithExtraHost("tfrm-kv-test.vault.topaz.local.dev", _containerTopaz.IpAddress)
                .WithExtraHost("tfrm-kv-sd.vault.topaz.local.dev", _containerTopaz.IpAddress)
                .WithExtraHost("tfrm-kv-keys.vault.topaz.local.dev", _containerTopaz.IpAddress)
                .WithExtraHost("tfrm-kv-test.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
                .WithExtraHost("tfrm-kv-sd.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
                .WithExtraHost("tfrm-kv-keys.keyvault.topaz.local.dev", _containerTopaz.IpAddress)
                // Table Storage data-plane: azurerm_storage_table/azurerm_storage_table_entity need
                // to connect to the table endpoint directly.
                .WithExtraHost("tfrmstortableacct.table.storage.topaz.local.dev", _containerTopaz.IpAddress)
                .WithExtraHost("tfisoentityacct.table.storage.topaz.local.dev", _containerTopaz.IpAddress)
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
                $"Container setup failed. STDOUT: {TruncateForOutput(setupResult.Stdout)}, STDERR: {TruncateForOutput(setupResult.Stderr)}");

            await WaitForTopazReadiness();

            await RunTerraformContainerCommand("az cloud register -n Topaz --cloud-config @\"cloud.json\"", maxAttempts: 5);
            await RunTerraformContainerCommand("az cloud set -n Topaz", maxAttempts: 3);
            await RunTerraformContainerCommand("az login --username topazadmin@topaz.local.dev --password admin", maxAttempts: 3);
            await EnsureSubscriptionExistsInTopaz();
            await RunTerraformContainerCommand($"az account set --subscription {_subscriptionId}", maxAttempts: 3);

            // Pre-initialize one workspace per provider so tests can skip `terraform init`.
            await PreInitializeProviderTemplates();

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

    // Subclasses can hook here to run teardown (e.g., terraform destroy) before containers are stopped.
    protected static event Action? BeforeContainerCleanup;

    private static void CleanupSharedResources()
    {
        try { BeforeContainerCleanup?.Invoke(); } catch { /* best-effort */ }
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

    // Synchronously destroys a Terraform workspace — safe to call from ProcessExit / event handlers.
    protected static void DestroyTerraformWorkspaceSync(string workDir)
    {
        try
        {
            _containerTerraform!.ExecAsync(new List<string>
            {
                "/bin/sh", "-c",
                $"SSL_CERT_FILE=/tmp/combined.pem terraform -chdir={workDir} destroy -auto-approve"
            }).GetAwaiter().GetResult();
        }
        catch
        {
            // Best-effort: container may already be stopping.
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
            // Fetch the full container log output — the failure may have occurred early
            // in the apply run, well outside a fixed --tail window. The filter below trims
            // the result to only interesting lines, keeping the output size manageable.
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"logs {containerName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            // docker writes its logs to stderr as well; merge both streams
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(15_000);

            if (process.ExitCode != 0)
                return string.Empty;

            var allLines = (stdout + "\n" + stderr).Split('\n');
            var interesting = allLines
                .Where(line =>
                    line.Contains("[Error]", StringComparison.Ordinal) ||
                    line.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("ListKeys for", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                .Take(400);

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
        var (outputs, workDir) = await ApplyTerraformRetaining(providerRelPath, scenarioRelPath);
        assertOutputs?.Invoke(outputs);
        await ExecTerraform($"terraform -chdir={workDir} destroy -auto-approve");
    }

    // Applies a scenario and returns (all outputs, workDir) WITHOUT destroying.
    // Used by AzureRmBatchFixture to apply once for all tests.
    protected async Task<(JsonNode Outputs, string WorkDir)> ApplyTerraformRetaining(
        string providerRelPath, string scenarioRelPath)
    {
        var workDir = $"/workspace/{Guid.NewGuid():N}";
        var terraformDir = Path.Combine(AppContext.BaseDirectory, "terraform");

        await ExecTerraform($"mkdir -p {workDir}");

        // Copy provider config
        await WriteFileToContainer(workDir, "provider.tf",
            await File.ReadAllTextAsync(Path.Combine(terraformDir, providerRelPath)));

        // Copy all scenario .tf files
        foreach (var tfFile in Directory.GetFiles(Path.Combine(terraformDir, scenarioRelPath), "*.tf"))
            await WriteFileToContainer(workDir, Path.GetFileName(tfFile),
                await File.ReadAllTextAsync(tfFile));

        // Copy the pre-initialized provider template — fast alternative to `terraform init`.
        var templateDir = GetTemplateDir(providerRelPath);
        await ExecTerraform(
            $"cp -a {templateDir}/.terraform {workDir}/.terraform && " +
            $"(cp {templateDir}/.terraform.lock.hcl {workDir}/.terraform.lock.hcl 2>/dev/null || true)");

        try
        {
            await ExecTerraform($"terraform -chdir={workDir} apply -auto-approve");
        }
        catch (AssertionException ex) when (IsRecoverableProviderStartupError(ex.Message))
        {
            // Corrupted provider binary. Purge cache, rebuild template, retry.
            await ExecTerraform($"rm -rf {workDir}/.terraform");
            await ExecTerraform($"rm -rf {templateDir}/.terraform");
            await ExecTerraform("find /tf-cache/plugin-cache -path '*registry.terraform.io/hashicorp/azurerm*' -exec rm -rf {} + || true");

            await ExecTerraformWithRetry(
                $"terraform -chdir={templateDir} init",
                maxAttempts: 3,
                shouldRetry: (_, stderr) => IsInitRetryableError(stderr),
                onRetry: async () =>
                {
                    await ExecTerraform($"rm -rf {templateDir}/.terraform");
                    await ExecTerraform("find /tf-cache/plugin-cache -path '*registry.terraform.io/hashicorp/azurerm*' -exec rm -rf {} + || true");
                });

            await ExecTerraform(
                $"cp -a {templateDir}/.terraform {workDir}/.terraform && " +
                $"(cp {templateDir}/.terraform.lock.hcl {workDir}/.terraform.lock.hcl 2>/dev/null || true)");

            await ExecTerraform($"terraform -chdir={workDir} apply -auto-approve");
        }

        var (stdout, _) = await ExecTerraformWithOutput($"terraform -chdir={workDir} output -json");
        return (JsonNode.Parse(stdout)!, workDir);
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
            Console.WriteLine(TruncateForOutput(result.Stdout));

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync(TruncateForOutput(result.Stderr));

            var topazLogs = ReadDockerLogs("topaz.local.dev", 400);
            if (!string.IsNullOrWhiteSpace(topazLogs))
                await Console.Error.WriteLineAsync($"[docker logs topaz.local.dev]\n{topazLogs}");

            // Extract relevant lines from the TF_LOG debug log (Authorization header, SharedKey, table/entity paths)
            var tfLogResult = await _containerTerraform!.ExecAsync(new List<string>
            {
                "/bin/sh", "-c",
                "grep -iE 'SharedKey|Authorization|table|entity|PartitionKey|RowKey|listKeys|table.storage|x-ms-date|content-type|content-md5|StringToSign|accountKey' /tmp/tf-debug.log 2>/dev/null | tail -200 || echo '[no tf-debug.log]'"
            });
            if (!string.IsNullOrWhiteSpace(tfLogResult.Stdout))
                await Console.Error.WriteLineAsync($"[TF debug log (filtered)]\n{TruncateForOutput(tfLogResult.Stdout, 20_000)}");

            Assert.That(result.ExitCode, Is.EqualTo(0),
                $"`{command}` failed.\nSTDOUT: {TruncateForOutput(result.Stdout)}\nSTDERR: {TruncateForOutput(result.Stderr)}" +
                (string.IsNullOrWhiteSpace(topazLogs) ? "" : $"\n[Topaz Docker logs]\n{topazLogs}"));
        }

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"`{command}` failed.\nSTDOUT: {TruncateForOutput(result.Stdout)}\nSTDERR: {TruncateForOutput(result.Stderr)}");

        return (result.Stdout, result.Stderr);
    }

    /// <summary>
    /// Truncates a string to at most <paramref name="maxChars"/> characters, keeping the first and
    /// last halves separated by an ellipsis marker. This prevents the NUnit TRX logger from writing
    /// multi-megabyte text nodes that exceed the XML parser limit used by the CI result publisher.
    /// </summary>
    private static string TruncateForOutput(string text, int maxChars = 8_000)
    {
        if (text.Length <= maxChars)
            return text;

        var half = maxChars / 2;
        return text[..half] +
               $"\n... [{text.Length - maxChars:N0} characters omitted] ...\n" +
               text[^half..];
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
            await Console.Error.WriteLineAsync(TruncateForOutput(result.Stderr));

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"`{command}` failed.\nSTDOUT: {TruncateForOutput(result.Stdout)}\nSTDERR: {TruncateForOutput(result.Stderr)}");
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

    // -------------------------------------------------------------------------
    // Provider template helpers — pre-init once, copy per test
    // -------------------------------------------------------------------------

    private static string GetTemplateDir(string providerRelPath) =>
        providerRelPath switch
        {
            _ when providerRelPath.Contains("azurerm") => TemplateAzureRm,
            _ when providerRelPath.Contains("azapi")   => TemplateAzureApi,
            _                                          => TemplateAzureAd,
        };

    private static bool IsInitRetryableError(string stderr) =>
        stderr.Contains("Failed to query available provider packages", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("could not connect to registry.terraform.io", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("context deadline exceeded", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("Failed to install provider", StringComparison.OrdinalIgnoreCase) ||
        stderr.Contains("unexpected EOF", StringComparison.OrdinalIgnoreCase);

    private async Task PreInitializeProviderTemplates()
    {
        var terraformDir = Path.Combine(AppContext.BaseDirectory, "terraform");
        var templates = new[]
        {
            (TemplateAzureRm,  "providers/azurerm.tf"),
            (TemplateAzureApi, "providers/azapi.tf"),
            (TemplateAzureAd,  "providers/azuread.tf"),
        };

        foreach (var (templateDir, providerFile) in templates)
        {
            await ExecTerraform($"mkdir -p {templateDir}");
            await WriteFileToContainer(templateDir, "provider.tf",
                await File.ReadAllTextAsync(Path.Combine(terraformDir, providerFile)));

            await ExecTerraformWithRetry(
                $"terraform -chdir={templateDir} init",
                maxAttempts: 3,
                shouldRetry: (_, stderr) => IsInitRetryableError(stderr),
                onRetry: async () =>
                {
                    await ExecTerraform($"rm -rf {templateDir}/.terraform");
                    await ExecTerraform("find /tf-cache/plugin-cache -path '*registry.terraform.io/hashicorp/azurerm*' -exec rm -rf {} + || true");
                });
        }
    }
}

using System.Collections.Concurrent;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Topaz.Tests.AzureCLI;

/// <summary>
/// Extends TopazFixture with a dedicated Ubuntu container that has azcopy installed.
/// Base class OneTimeSetUp (Topaz + Azure CLI) runs first; this setup runs second.
/// </summary>
public abstract class AzCopyFixture : TopazFixture
{
    private static readonly string CertificateFile = File.ReadAllText("topaz.crt");

    private IContainer? _containerAzCopy;
    private readonly ConcurrentHashSet _mappedHosts = new();

    [OneTimeSetUp]
    public async Task AzCopySetUp()
    {
        _containerAzCopy = new ContainerBuilder()
            .WithImage("ubuntu:22.04")
            .WithNetwork(ContainerNetwork)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(System.Text.Encoding.UTF8.GetBytes(CertificateFile), "/tmp/topaz.crt")
            .WithEnvironment("AZCOPY_AUTO_LOGIN_TYPE", "AZCOPY_SAS")
            .Build();

        await _containerAzCopy.StartAsync();

        // Install curl, azcopy, and trust the Topaz certificate
        await ExecAzCopy("apt-get update -y -qq && apt-get install -y -qq curl ca-certificates 2>/dev/null");
        await ExecAzCopy(
            "ARCH=$(uname -m) && " +
            "if [ \"$ARCH\" = \"aarch64\" ]; then URL=\"https://aka.ms/downloadazcopy-v10-linux-arm64\"; " +
            "else URL=\"https://aka.ms/downloadazcopy-v10-linux\"; fi && " +
            "curl -sL \"$URL\" | tar -xz --strip-components=1 --wildcards -C /usr/local/bin '*/azcopy' " +
            "&& chmod +x /usr/local/bin/azcopy");
        await ExecAzCopy(
            "cp /tmp/topaz.crt /usr/local/share/ca-certificates/topaz.crt " +
            "&& update-ca-certificates");
    }

    [OneTimeTearDown]
    public async Task AzCopyTearDown()
    {
        if (_containerAzCopy != null)
            await _containerAzCopy.DisposeAsync();
    }

    /// <summary>
    /// Ensures {accountName}.blob.storage.topaz.local.dev → Topaz IP in the azcopy container's /etc/hosts.
    /// </summary>
    protected async Task EnsureAzCopyBlobHostMapping(string accountName)
    {
        var host = $"{accountName}.blob.storage.topaz.local.dev";
        if (!_mappedHosts.TryAdd(host)) return;

        await ExecAzCopy(
            $"grep -qF '{host}' /etc/hosts || echo '{TopazContainerIpAddress} {host}' >> /etc/hosts",
            assertExitCode: true);
    }

    /// <summary>
    /// Ensures {accountName}.blob.storage.topaz.local.dev → Topaz IP in the Azure CLI container's /etc/hosts.
    /// Needed when commands use --connection-string (account name is in the URL, not --account-name).
    /// </summary>
    protected async Task EnsureAzureCliBlobHostMapping(string accountName)
    {
        var host = $"{accountName}.blob.storage.topaz.local.dev";
        await AzureCliContainer.ExecAsync([
            "/bin/sh", "-c",
            $"grep -qF '{host}' /etc/hosts || echo '{TopazContainerIpAddress} {host}' >> /etc/hosts"
        ]);
    }

    /// <summary>
    /// Runs a shell command inside the azcopy container and asserts the exit code.
    /// </summary>
    protected async Task RunAzCopyCommand(string command, long exitCode = 0)
    {
        var (stdout, stderr, actual) = await ExecAzCopy(command, assertExitCode: false);

        Console.WriteLine($"[azcopy] {command}");
        if (actual == 0)
        {
            Console.WriteLine($"[azcopy] STDOUT: {stdout}");
        }
        else
        {
            await Console.Error.WriteLineAsync($"[azcopy] STDERR: {stderr}");
            
            // Dump the latest azcopy job log for diagnosis
            var (logContent, _, _) = await ExecAzCopy(
                "cat $(ls -t /root/.azcopy/*.log 2>/dev/null | head -1) 2>/dev/null || echo '(no azcopy log found)'",
                assertExitCode: false);
            await Console.Error.WriteLineAsync($"[azcopy] JOB LOG:\n{logContent}");
        }
        
        Assert.That(actual, Is.EqualTo(exitCode),
            $"azcopy command failed.\nCOMMAND: {command}\nSTDOUT: {stdout}\nSTDERR: {stderr}");
    }

    /// <summary>
    /// Runs an az CLI command and returns the raw stdout (not parsed as JSON).
    /// </summary>
    protected async Task<string> GetAzureCliRawOutput(string command)
    {
        var result = await AzureCliContainer.ExecAsync(["/bin/sh", "-c", command]);
        Assert.That(result.ExitCode, Is.EqualTo(0L),
            $"CLI command failed.\nCOMMAND: {command}\nSTDERR: {result.Stderr}");
        return result.Stdout.Trim();
    }

    private async Task<(string Stdout, string Stderr, long ExitCode)> ExecAzCopy(
        string command, bool assertExitCode = true)
    {
        var result = await _containerAzCopy!.ExecAsync(["/bin/sh", "-c", command]);
        if (assertExitCode)
            Assert.That(result.ExitCode, Is.EqualTo(0),
                $"azcopy container setup command failed.\nCOMMAND: {command}\nSTDERR: {result.Stderr}");
        return (result.Stdout, result.Stderr, result.ExitCode);
    }

    // Simple thread-safe set for host mapping deduplication
    private sealed class ConcurrentHashSet
    {
        private readonly System.Collections.Generic.HashSet<string> _set = new();
        private readonly object _lock = new();
        public bool TryAdd(string item) { lock (_lock) { return _set.Add(item); } }
    }
}

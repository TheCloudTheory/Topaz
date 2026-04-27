using System.Text.RegularExpressions;
using DotNet.Testcontainers.Containers;

namespace Topaz.Tests.AzurePowerShell;

internal static class KeyVaultHostMapper
{
    private static readonly Regex VaultNameArgumentPattern =
        new(@"-VaultName\s+(?:""(?<name>[^""]+)""|'(?<name>[^']+)'|(?<name>\S+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> _mappedHosts =
        new(StringComparer.OrdinalIgnoreCase);

    internal static void ResetCache() { lock (_mappedHosts) { _mappedHosts.Clear(); } }

    public static async Task EnsureVaultHostsMapped(
        IContainer powerShellContainer,
        IContainer topazContainer,
        string script)
    {
        var vaultNames = VaultNameArgumentPattern.Matches(script)
            .Select(m => m.Groups["name"].Value.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var vaultName in vaultNames)
        {
            await EnsureHostMapping(powerShellContainer, topazContainer,
                $"{vaultName.ToLowerInvariant()}.vault.topaz.local.dev");
            await EnsureHostMapping(powerShellContainer, topazContainer,
                $"{vaultName.ToLowerInvariant()}.keyvault.topaz.local.dev");

            // Also map the original casing if it differs
            if (!vaultName.Equals(vaultName.ToLowerInvariant(), StringComparison.Ordinal))
            {
                await EnsureHostMapping(powerShellContainer, topazContainer,
                    $"{vaultName}.vault.topaz.local.dev");
                await EnsureHostMapping(powerShellContainer, topazContainer,
                    $"{vaultName}.keyvault.topaz.local.dev");
            }
        }
    }

    private static async Task EnsureHostMapping(
        IContainer powerShellContainer,
        IContainer topazContainer,
        string host)
    {
        // Check the in-memory cache first to avoid redundant /etc/hosts reads
        lock (_mappedHosts)
        {
            if (_mappedHosts.Contains(host))
                return;
        }

        var result = await powerShellContainer.ExecAsync(new List<string>
        {
            "/bin/sh", "-c",
            $"grep -qiE '(^|[[:space:]]){Regex.Escape(host)}([[:space:]]|$)' /etc/hosts " +
            $"|| echo '{topazContainer.IpAddress} {host}' >> /etc/hosts"
        });

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"Failed to register host mapping for {host}. STDERR: {result.Stderr}");

        // Only cache after the mapping was confirmed successful
        lock (_mappedHosts)
        {
            _mappedHosts.Add(host);
        }
    }
}

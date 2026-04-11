using System.Text.RegularExpressions;
using DotNet.Testcontainers.Containers;

namespace Topaz.Tests.AzureCLI;

internal static class KeyVaultHostMapper
{
    private static readonly Regex VaultNameArgumentPattern =
        new("--vault-name\\s+(?:\"(?<name>[^\"]+)\"|(?<name>\\S+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task EnsureVaultHostsMapped(IContainer azureCliContainer, IContainer topazContainer, string command)
    {
        var vaultNames = VaultNameArgumentPattern.Matches(command)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var vaultName in vaultNames)
        {
            await EnsureHostMapping(azureCliContainer, topazContainer, $"{vaultName}.keyvault.topaz.local.dev");

            if (!vaultName.Equals(vaultName.ToLowerInvariant(), StringComparison.Ordinal))
            {
                await EnsureHostMapping(azureCliContainer, topazContainer, $"{vaultName.ToLowerInvariant()}.keyvault.topaz.local.dev");
            }
        }
    }

    private static async Task EnsureHostMapping(IContainer azureCliContainer, IContainer topazContainer, string host)
    {
        var mappingResult = await azureCliContainer.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            $"grep -qiE '(^|[[:space:]]){Regex.Escape(host)}([[:space:]]|$)' /etc/hosts || echo '{topazContainer.IpAddress} {host}' >> /etc/hosts"
        });

        Assert.That(mappingResult.ExitCode, Is.EqualTo(0),
            $"Failed to register host mapping for {host}. STDERR: {mappingResult.Stderr}");
    }
}
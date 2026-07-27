using System.Text.RegularExpressions;
using DotNet.Testcontainers.Containers;

namespace Topaz.Tests.AzureCLI;

internal static class AppConfigurationHostMapper
{
    // Matches --name / -n for "az appconfig kv ..." commands
    private static readonly Regex StoreNamePattern =
        new(@"az\s+appconfig\s+kv\s+\S+.*?(?:--name|-n)\s+(?:""(?<name>[^""]+)""|(?<name>\S+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    public static async Task EnsureAppConfigHostsMapped(IContainer azureCliContainer, IContainer topazContainer, string command)
    {
        if (!command.Contains("appconfig kv", StringComparison.OrdinalIgnoreCase)) return;

        var storeNames = StoreNamePattern.Matches(command)
            .Select(m => m.Groups["name"].Value.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var name in storeNames)
        {
            await EnsureHostMapping(azureCliContainer, topazContainer, $"{name}.azconfig.topaz.local.dev");
            await EnsureHostMapping(azureCliContainer, topazContainer, $"{name.ToLowerInvariant()}.azconfig.topaz.local.dev");
        }
    }

    private static async Task EnsureHostMapping(IContainer azureCliContainer, IContainer topazContainer, string host)
    {
        var result = await azureCliContainer.ExecAsync(new List<string>
        {
            "/bin/sh",
            "-c",
            $"grep -qiE '(^|[[:space:]]){Regex.Escape(host)}([[:space:]]|$)' /etc/hosts || echo '{topazContainer.IpAddress} {host}' >> /etc/hosts"
        });

        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"Failed to register host mapping for {host}. STDERR: {result.Stderr}");
    }
}

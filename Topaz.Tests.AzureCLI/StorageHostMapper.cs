using System.Text.RegularExpressions;
using DotNet.Testcontainers.Containers;

namespace Topaz.Tests.AzureCLI;

internal static class StorageHostMapper
{
    private static readonly Regex AccountNameArgumentPattern =
        new(@"--account-name\s+(?:""(?<name>[^""]+)""|(?<name>\S+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static async Task EnsureStorageHostsMapped(IContainer azureCliContainer, IContainer topazContainer, string command)
    {
        var accountNames = AccountNameArgumentPattern.Matches(command)
            .Select(match => match.Groups["name"].Value.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var accountName in accountNames)
        {
            await EnsureHostMapping(azureCliContainer, topazContainer, $"{accountName}.table.storage.topaz.local.dev");
            await EnsureHostMapping(azureCliContainer, topazContainer, $"{accountName}.blob.storage.topaz.local.dev");
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

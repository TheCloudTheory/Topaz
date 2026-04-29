using System.Text.Json.Nodes;
using DotNet.Testcontainers.Containers;

namespace Topaz.Tests.AzurePowerShell;

/// <summary>
/// Base class for all AzurePowerShell test classes.
/// Provides <see cref="RunAzurePowerShellCommand"/> against the shared containers
/// started by <see cref="TopazPowerShellFixture"/>.
/// </summary>
public abstract class PowerShellTestBase
{
    protected async Task RunAzurePowerShellCommand(
        string script,
        Action<JsonNode>? assertion = null,
        int exitCode = 0)
    {
        var containerPs   = TopazPowerShellFixture.ContainerPowerShell;
        var containerTopaz = TopazPowerShellFixture.ContainerTopaz;

        if (containerPs != null && containerTopaz != null)
        {
            await KeyVaultHostMapper.EnsureVaultHostsMapped(containerPs, containerTopaz, script);
        }

        var wrappedScript = BuildAuthenticatedScript(script);

        var result = await containerPs!.ExecAsync(new List<string>
        {
            "pwsh", "-NonInteractive", "-Command", wrappedScript
        });

        Console.WriteLine($"Script: {script}");

        if (result.ExitCode == 0)
        {
            Console.WriteLine($"STDOUT: {result.Stdout}");
        }

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync($"STDERR: {result.Stderr}");
        }

        Assert.That(result.ExitCode, Is.EqualTo(exitCode),
            $"PowerShell script failed. STDOUT: {result.Stdout}, STDERR: {result.Stderr}");

        if (assertion != null)
        {
            // Az cmdlets may emit WARNING:/VERBOSE: lines before the JSON output.
            // Find the first JSON-starting character so the parser is not tripped up
            // by any non-JSON preamble that slips through.
            var stdout = result.Stdout ?? string.Empty;
            var jsonStart = stdout.IndexOfAny(['{', '[']);
            var jsonText = jsonStart >= 0 ? stdout[jsonStart..] : stdout;
            assertion.Invoke(JsonNode.Parse(jsonText)!);
        }
    }

    private static string BuildAuthenticatedScript(string script)
    {
        var escapedScript = script.Replace("'@", "'@@");

        return string.Join('\n',
            "$PSStyle.OutputRendering = 'PlainText'",
            "$ErrorActionPreference     = \"Stop\"",
            "$WarningPreference         = 'SilentlyContinue'",
            "$InformationPreference     = 'SilentlyContinue'",
            "& /tmp/restore-az-context.ps1",
            "if ($null -eq (Get-AzContext)) { throw \"No Azure context after restore script.\" }",
            "$scriptToRun = @'",
            escapedScript,
            "'@",
            "Invoke-Expression $scriptToRun");
    }
}

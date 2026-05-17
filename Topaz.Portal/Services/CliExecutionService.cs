using System.Diagnostics;
using System.Text;
using Topaz.Portal.Models.Cli;

namespace Topaz.Portal.Services;

public sealed class CliExecutionService : ICliExecutionService
{
    private readonly HashSet<string> _validCommands;
    private readonly ILogger<CliExecutionService> _logger;

    public CliExecutionService(ICliSuggestionService suggestionService, ILogger<CliExecutionService> logger)
    {
        _logger = logger;
        _validCommands = suggestionService
            .GetAll()
            .Select(c => c.Name.Split(' ')[0].Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<CliExecutionResult> ExecuteAsync(string commandLine, CancellationToken cancellationToken = default)
    {
        var args = ParseArgs(commandLine);

        if (args.Length == 0)
            return new CliExecutionResult("Empty command.", IsError: true);

        if (!_validCommands.Contains(args[0]))
            return new CliExecutionResult(
                $"Unknown command: '{args[0]}'. Only Topaz CLI commands are allowed.",
                IsError: true);

        var binary = FindTopazBinary();
        if (binary is null)
            return new CliExecutionResult(
                "topaz binary not found. Install Topaz CLI or ensure it is on the system PATH.",
                IsError: true);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = binary,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = AppContext.BaseDirectory
            };

            foreach (var arg in args)
                startInfo.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(30));

            var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

            await process.WaitForExitAsync(linkedCts.Token);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
                return new CliExecutionResult(
                    string.IsNullOrWhiteSpace(stderr) ? stdout.TrimEnd() : stderr.TrimEnd(),
                    IsError: true);

            return new CliExecutionResult(stdout.TrimEnd(), IsError: false);
        }
        catch (OperationCanceledException)
        {
            return new CliExecutionResult("Command timed out after 30 seconds.", IsError: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing CLI command: {CommandLine}", commandLine);
            return new CliExecutionResult($"Execution error: {ex.Message}", IsError: true);
        }
    }

    private static string? FindTopazBinary()
    {
        // Check same directory first — used in Docker where the binary is bundled with the app
        var localPath = Path.Combine(AppContext.BaseDirectory, "topaz");
        if (File.Exists(localPath))
            return localPath;

        // Fall back to PATH — used in local development where Topaz CLI is installed on the host
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        var binaryName = OperatingSystem.IsWindows() ? "topaz.exe" : "topaz";

        foreach (var dir in pathEnv.Split(separator))
        {
            var candidate = Path.Combine(dir.Trim(), binaryName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static string[] ParseArgs(string commandLine)
    {
        var args = new List<string>();
        var current = new StringBuilder();
        char? quoteChar = null;

        foreach (var c in commandLine.Trim())
        {
            if (quoteChar.HasValue)
            {
                if (c == quoteChar.Value)
                    quoteChar = null;
                else
                    current.Append(c);
            }
            else if (c is '"' or '\'')
            {
                quoteChar = c;
            }
            else if (c == ' ')
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            args.Add(current.ToString());

        return [.. args];
    }
}

using System.Diagnostics;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Executors;

internal abstract class ExecutorBase(ITopazLogger logger)
{
    protected async Task<bool> RunProcessAsync(
        string fileName,
        string arguments,
        string logPath,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        // We need to transform the arguments into a collection of strings that can be passed to ProcessStartInfo.
        // The reason for this is that some arguments may contain quoted strings. Those are treated as
        // separate arguments, e.g., bash -c 'echo $(date) >> hello.txt' would be handled as bash, -c, 'echo, $(date),
        // etc. eventually breaking the whole command.
        var argumentsCollection = arguments.Split(' ');
        var quotedArguments = arguments.Split('\'');

        if (quotedArguments.Length > 1)
        {
            // If there are any quoted arguments, the split above will give us an array
            // of two elements. We need to keep the first half as a collection and
            // append the rest as a single string.
            argumentsCollection = [.. quotedArguments[0].Split(' ', StringSplitOptions.RemoveEmptyEntries), string.Join(" ", quotedArguments[1..])];
        }
        
        var psi = new ProcessStartInfo(fileName, argumentsCollection)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = new Process();
        process.StartInfo = psi;
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                AppendLogAsync(logPath, e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                AppendLogAsync(logPath, e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }
    
    protected void AppendLogAsync(string logPath, string line)
    {
        logger.LogDebug(nameof(ExecutorBase), nameof(AppendLogAsync), line);
        
        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    protected static string GenerateTempDir(string? runId = null)
    {
        var uuid = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N")[..8] : runId;
        var tempPath = Path.Combine(Path.GetTempPath(), "topaz-acr-" + uuid);
        
        if(!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        
        return tempPath;
    }
}
using System.Diagnostics;

namespace Topaz.Service.ContainerRegistry.Executors;

internal abstract class ExecutorBase
{
    protected static async Task<bool> RunProcessAsync(
        string fileName,
        string arguments,
        string logPath,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var argumentsCollection = arguments.Split(' ');
        var quotedArguments = arguments.Split('\'');

        if (quotedArguments.Length > 1)
        {
            // If there are any quoted arguments, the split above will give us an array
            // of two elements. We need to keep the first half as a collection and
            // append the rest as a single string.
            argumentsCollection = [.. quotedArguments[0].Split(' '), string.Join(" ", quotedArguments[1..])];
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
            if (e.Data != null) File.AppendAllText(logPath, e.Data + Environment.NewLine);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) File.AppendAllText(logPath, e.Data + Environment.NewLine);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }

    protected static Task AppendLogAsync(string logPath, string line)
    {
        File.AppendAllText(logPath, line + Environment.NewLine);
        return Task.CompletedTask;
    }

    protected static string GenerateTempDir()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "topaz-acr-" + Guid.NewGuid().ToString("N")[..8]);
        if(!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        
        return tempPath;
    }
}
using System.Diagnostics;

namespace Topaz.Service.ContainerRegistry.Executors;

internal abstract class ExecutorBase
{
    protected static async Task<bool> RunProcessAsync(
        string fileName,
        string arguments,
        string logPath,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
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
        return Path.Combine(Path.GetTempPath(), "topaz-acr-" + Guid.NewGuid().ToString("N")[..8]);
    }
}
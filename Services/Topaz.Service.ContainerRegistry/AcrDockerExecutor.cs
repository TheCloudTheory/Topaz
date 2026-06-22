using System.Diagnostics;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry;

/// <summary>
/// Shells out to the host Docker daemon to execute a DockerBuildRequest ACR run.
/// All other step types are handled by the existing immediate-Succeeded path.
/// </summary>
public static class AcrDockerExecutor
{
    private static readonly Lazy<bool> _available = new(CheckAvailability);

    public static bool IsAvailable() => _available.Value;

    /// <summary>
    /// Runs <c>docker build</c> (and optionally <c>docker push</c>) for a DockerBuildRequest run.
    /// Appends stdout/stderr to <paramref name="logPath"/> line-by-line.
    /// Returns <c>true</c> on success, <c>false</c> on non-zero exit or exception.
    /// </summary>
    public static async Task<bool> ExecuteAsync(
        string contextPath,
        string dockerFilePath,
        string imageName,
        bool isPushEnabled,
        string logPath,
        CancellationToken cancellationToken)
    {
        string buildContext;
        string? tempDir = null;

        try
        {
            if (contextPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                contextPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                tempDir = Path.Combine(Path.GetTempPath(), "topaz-acr-" + Guid.NewGuid().ToString("N")[..8]);
                await AppendLogAsync(logPath, $"Cloning context from {contextPath}...");
                var cloneOk = await RunProcessAsync("git", $"clone {contextPath} \"{tempDir}\"", logPath, cancellationToken);
                if (!cloneOk) return false;
                buildContext = tempDir;
            }
            else
            {
                buildContext = contextPath;
            }

            var buildArgs = $"build -f \"{dockerFilePath}\" -t \"{imageName}\" \"{buildContext}\"";
            await AppendLogAsync(logPath, $"Running: docker {buildArgs}");
            var buildOk = await RunProcessAsync("docker", buildArgs, logPath, cancellationToken);
            if (!buildOk) return false;

            if (isPushEnabled)
            {
                await AppendLogAsync(logPath, $"Running: docker push \"{imageName}\"");
                var pushOk = await RunProcessAsync("docker", $"push \"{imageName}\"", logPath, cancellationToken);
                if (!pushOk) return false;
            }

            await AppendLogAsync(logPath, "Run completed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            await AppendLogAsync(logPath, $"Error: {ex.Message}");
            return false;
        }
        finally
        {
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { /* best-effort cleanup */ }
            }
        }
    }

    private static async Task<bool> RunProcessAsync(
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

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
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

    private static Task AppendLogAsync(string logPath, string line)
    {
        File.AppendAllText(logPath, line + Environment.NewLine);
        return Task.CompletedTask;
    }

    private static bool CheckAvailability()
    {
        try
        {
            var psi = new ProcessStartInfo("docker", "info")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(5_000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

using System.Diagnostics;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Executors;

/// <summary>
/// Shells out to the host Docker daemon to execute a DockerBuildRequest ACR run.
/// All other step types are handled by the existing immediate-Succeeded path.
/// </summary>
[UsedImplicitly]
internal class AcrDockerExecutor(ITopazLogger logger) : ExecutorBase(logger)
{
    private static readonly Lazy<bool> Available = new(CheckAvailability);

    public static bool IsAvailable() => Available.Value;

    /// <summary>
    /// Runs <c>docker build</c> (and optionally <c>docker push</c>) for a DockerBuildRequest run.
    /// Appends stdout/stderr to <paramref name="logPath"/> line-by-line.
    /// Returns <c>true</c> on success, <c>false</c> on non-zero exit or exception.
    /// </summary>
    public async Task<bool> ExecuteAsync(
        string contextPath,
        string dockerFilePath,
        string imageName,
        bool isPushEnabled,
        string logPath,
        CancellationToken cancellationToken)
    {
        string? tempDir = null;

        try
        {
            string buildContext;
            if (contextPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                contextPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                tempDir = GenerateTempDir();
                await AppendLogAsync(logPath, $"Cloning context from {contextPath}...");
                var cloneOk = await RunProcessAsync("git", $"clone {contextPath} \"{tempDir}\"", logPath, null, cancellationToken);
                if (!cloneOk) return false;
                buildContext = tempDir;
            }
            else
            {
                buildContext = contextPath;
            }

            var buildArgs = $"build -f \"{dockerFilePath}\" -t \"{imageName}\" \"{buildContext}\"";
            await AppendLogAsync(logPath, $"Running: docker {buildArgs}");
            var buildOk = await RunProcessAsync("docker", buildArgs, logPath, null, cancellationToken);
            if (!buildOk) return false;

            if (isPushEnabled)
            {
                await AppendLogAsync(logPath, $"Running: docker push \"{imageName}\"");
                var pushOk = await RunProcessAsync("docker", $"push \"{imageName}\"", logPath, null, cancellationToken);
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

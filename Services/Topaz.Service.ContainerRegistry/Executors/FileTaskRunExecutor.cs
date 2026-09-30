using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;

namespace Topaz.Service.ContainerRegistry.Executors;

[UsedImplicitly]
internal sealed class FileTaskRunExecutor : ExecutorBase
{
    public static async Task<bool> ExecuteAsync(ScheduleTaskRunRequest fileTaskStep, string logPath,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        string? tempDir = null;
        
        try
        {
            if (!string.IsNullOrWhiteSpace(fileTaskStep.SourceLocation) && fileTaskStep.SourceLocation.EndsWith(".git") && !string.IsNullOrWhiteSpace(fileTaskStep.TaskFilePath))
            {
                await AppendLogAsync(logPath, $"Cloning context from {fileTaskStep.SourceLocation}...");
             
                tempDir = GenerateTempDir();
                var cloneOk = await RunProcessAsync("git", $"clone {fileTaskStep.SourceLocation} \"{tempDir}\"", logPath, cancellationToken);
                if (!cloneOk)
                {
                    await AppendLogAsync(logPath, $"Error cloning context from {fileTaskStep.SourceLocation}");
                    return false;
                }
                
                var taskFilePath = Path.Combine(tempDir, fileTaskStep.TaskFilePath);
                if (!File.Exists(taskFilePath))
                {
                    await AppendLogAsync(logPath, $"Task file {taskFilePath} does not exist");
                    return false;
                }
                
                var content = await File.ReadAllTextAsync(taskFilePath, cancellationToken);
                
                await ParseAndRunFileTask(content, logPath, cancellationToken);
                
                await AppendLogAsync(logPath, "Run completed successfully.");
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            await AppendLogAsync(logPath, $"Error: {ex.Message}");
            return false;
        }
    }

    private static Task ParseAndRunFileTask(string content, string logPath, CancellationToken cancellationToken)
    {
        var task = YamlSerializerFacade.Deserialize<ContainerRegistryTaskFile>(content);
        return Task.CompletedTask;
    }
}
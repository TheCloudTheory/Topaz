using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Executors;

[UsedImplicitly]
internal sealed class FileTaskRunExecutor(ITopazLogger logger) : ExecutorBase(logger)
{
    private readonly ITopazLogger _logger = logger;

    public async Task<bool> ExecuteAsync(ScheduleTaskRunRequest fileTaskStep, string logPath,
        string registryName,
        string runId,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(fileTaskStep.SourceLocation) && fileTaskStep.SourceLocation.EndsWith(".git") && !string.IsNullOrWhiteSpace(fileTaskStep.TaskFilePath))
            {
                AppendLogAsync(logPath, $"Cloning context from {fileTaskStep.SourceLocation}...");
             
                var tempDir = GenerateTempDir(runId);
                var cloneOk = await RunProcessAsync("git", $"clone {fileTaskStep.SourceLocation} {tempDir}", logPath, tempDir, cancellationToken);
                if (!cloneOk)
                {
                    AppendLogAsync(logPath, $"Error cloning context from {fileTaskStep.SourceLocation}");
                    return false;
                }
                
                var taskFilePath = Path.Combine(tempDir, fileTaskStep.TaskFilePath);
                if (!File.Exists(taskFilePath))
                {
                    AppendLogAsync(logPath, $"Task file {taskFilePath} does not exist");
                    return false;
                }
                
                var content = await File.ReadAllTextAsync(taskFilePath, cancellationToken);
                var result = await ParseAndRunFileTask(content, logPath, registryName, runId, tempDir, cancellationToken);
                if (!result)
                {
                    AppendLogAsync(logPath, $"Error parsing and running file task.");
                    return false;
                }
                
                AppendLogAsync(logPath, "Run completed successfully.");
            }

            return true;
        }
        catch (Exception ex)
        {
            AppendLogAsync(logPath, $"Error: {ex.Message}");
            _logger.LogError(nameof(FileTaskRunExecutor), nameof(ExecuteAsync), $"Error parsing and running file task: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ParseAndRunFileTask(string content, string logPath, string registryName,
        string runId,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var task = YamlSerializerFacade.Deserialize<ContainerRegistryTaskFile>(content);
        if (task.Steps == null)
        {
            AppendLogAsync(logPath, "No steps defined.");
            return false;
        }

        var aliases = new Dictionary<string, string>();
        if (task.Alias != null)
        {
            AppendLogAsync(logPath, $"Resolving aliases.");
            aliases = ResolveAliases(task.Alias);
        }

        foreach (var step in task.Steps)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                AppendLogAsync(logPath, "Run cancelled.");
                return false;
            }

            var result = await ParseAndExecuteStep(step, logPath, registryName, runId, workingDirectory, aliases, cancellationToken);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, string> ResolveAliases(ContainerRegistryTaskAliases aliases)
    {
        var aliasesResolved = new Dictionary<string, string>();
        if (aliases.Values != null)
        {
            foreach (var value in aliases.Values)
            {
                aliasesResolved.Add(value.Key, value.Value);
            }
        }
        
        return aliasesResolved;
    }

    private async Task<bool> ParseAndExecuteStep(ContainerRegistryTaskStep step, string logPath,
        string registryName,
        string runId,
        string workingDirectory,
        Dictionary<string, string> aliases,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(step.Build))
        {
            AppendLogAsync(logPath, $"Building image: {step.Build}");
            return await BuildImage(step.Build, registryName, logPath, runId, workingDirectory, aliases, cancellationToken);
        }
        
        if(step.Push != null && step.Push.All(p => !string.IsNullOrWhiteSpace(p)))
        {
            AppendLogAsync(logPath, $"Pushing image: {string.Join(", ", step.Push)}");
            return await PushImage(step.Push, registryName, logPath, runId, workingDirectory, aliases, cancellationToken);
        }

        if (!string.IsNullOrEmpty(step.Cmd))
        {
            AppendLogAsync(logPath, $"Running command: {step.Cmd}");
            return await RunCommand(step.Cmd, registryName, logPath, runId, workingDirectory, aliases, cancellationToken);
        }
        
        return false;
    }

    private async Task<bool> RunCommand(string stepCmd, string registryName, string logPath, string runId,
        string workingDirectory, Dictionary<string, string> aliases, CancellationToken cancellationToken)
    {
        var compiledStep = CompileCommonPlaceholders(stepCmd, registryName, runId, aliases);
        var commandSegments = compiledStep.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var image = commandSegments[0];

        // If CMD is `docker`, assume it's a Docker command and run it directly
        if (image == "docker")
        {
            return await RunProcessAsync("docker", string.Join(" ", commandSegments.Skip(1)), logPath, workingDirectory, cancellationToken);
        }

        // It's possible that the command is `az`, assume it's an Azure CLI command and run it directly
        if (image == "az")
        {
            return await RunProcessAsync("az", string.Join(" ", commandSegments.Skip(1)), logPath, workingDirectory, cancellationToken);
        }
        
        // If no additional arguments of a command are provided, do not attempt to pass them
        if (commandSegments.Length <= 1)
        {
            return await RunProcessAsync("docker", $"run --rm --volume {workingDirectory}:/workspace --workdir /workspace {image}", logPath, workingDirectory, cancellationToken);
        }
        
        return await RunProcessAsync("docker", $"run --rm --volume {workingDirectory}:/workspace --workdir /workspace {image} {string.Join(" ", commandSegments.Skip(1))}", logPath, workingDirectory, cancellationToken);
    }

    private async Task<bool> PushImage(List<string> stepPush, string registryName, string logPath, string runId,
        string workingDirectory, Dictionary<string, string> aliases, CancellationToken cancellationToken)
    {
        foreach (var compiledStep in stepPush.Select(step => CompileCommonPlaceholders(step, registryName, runId, aliases)))
        {
            var result = await RunProcessAsync("docker", "push " + compiledStep, logPath, workingDirectory, cancellationToken);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> BuildImage(string stepBuild, string registryName, string logPath,
        string runId,
        string workingDirectory,
        Dictionary<string, string> aliases,
        CancellationToken cancellationToken)
    {
        var compiledStep = CompileCommonPlaceholders(stepBuild, registryName, runId, aliases);
        var result = await RunProcessAsync("docker", "build " + compiledStep, logPath, workingDirectory, cancellationToken);
        
        return result;
    }

    private static string CompileCommonPlaceholders(string stepBuild, string registryName, string runId,
        Dictionary<string, string> aliases)
    {
        // The step definition may contain "$Registry" placeholder which
        // needs to be replaced with the actual registry name.
        var compiledStep = stepBuild.Replace("$Registry", GlobalSettings.GetContainerRegistryEndpoint(registryName));

        // We also need to replace "$RunId" placeholder with the actual run id.
        compiledStep = compiledStep.Replace("$ID", runId);
        
        compiledStep = compiledStep.Replace("$Date", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        // Also replace aliases if any is provided
        return aliases.Aggregate(compiledStep, (current, alias) => current.Replace("$" + alias.Key, alias.Value));
    }
}
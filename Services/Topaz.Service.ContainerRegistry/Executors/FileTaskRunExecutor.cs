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
    public bool IsAvailable() => CheckAvailability();

    public async Task<bool> ExecuteAsync(ScheduleTaskRunRequest request, string logPath,
        string registryName,
        string runId,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            var tempDir = GenerateTempDir(runId);
            var content = string.Empty;
            
            if (!string.IsNullOrWhiteSpace(request.SourceLocation) &&
                request.SourceLocation.EndsWith(".git") &&
                !string.IsNullOrWhiteSpace(request.TaskFilePath))
            {
                AppendLogAsync(logPath, $"Cloning context from {request.SourceLocation}...");
            
                var cloneOk = await RunProcessAsync("git", $"clone {request.SourceLocation} {tempDir}", logPath,
                    tempDir, cancellationToken);
                if (!cloneOk)
                {
                    AppendLogAsync(logPath, $"Error cloning context from {request.SourceLocation}");
                    return false;
                }

                var taskFilePath = Path.Combine(tempDir, request.TaskFilePath);
                if (!File.Exists(taskFilePath))
                {
                    AppendLogAsync(logPath, $"Task file {taskFilePath} does not exist");
                    return false;
                }
                
                content = await File.ReadAllTextAsync(taskFilePath, cancellationToken);
            }

            // For EncodedTaskRunRequest the content is passed after being decoded 
            // from the base64 encoded string. It reuses the same model though for
            // simplicity.
            if (!string.IsNullOrWhiteSpace(request.TaskFileContent))
            {
                content = request.TaskFileContent;
            }
            
            var result = await ParseAndRunFileTask(content, logPath, registryName, runId, tempDir, username,
                password, cancellationToken);
            if (!result)
            {
                AppendLogAsync(logPath, $"Error parsing and running file task.");
                return false;
            }

            AppendLogAsync(logPath, "Run completed successfully.");

            return true;
        }
        catch (WrappedYamlException ex)
        {
            AppendLogAsync(logPath, ex.ToString());

            _logger.LogError(nameof(FileTaskRunExecutor), nameof(ExecuteAsync), $"Error parsing and running file task: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            AppendLogAsync(logPath,
                _logger.LogLevel == LogLevel.Debug ? $"Error: {ex.Message} {ex.StackTrace}" : $"Error: {ex.Message}");

            _logger.LogError(nameof(FileTaskRunExecutor), nameof(ExecuteAsync), $"Error parsing and running file task: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ParseAndRunFileTask(string content, string logPath, string registryName,
        string runId,
        string workingDirectory,
        string username,
        string password,
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
        
        // The initial validation passed. Before we start executing anything 
        // against the Docker Engine, we need to authorize the operations
        // using `docker login`
        var loginSuccessful = await RunProcessAsync("docker",
            $"login {GlobalSettings.GetContainerRegistryEndpoint(registryName)} -u {username} -p {password}",
            logPath, workingDirectory, cancellationToken);
        
        if (!loginSuccessful)
        {
            AppendLogAsync(logPath, $"Login failed.");
            return false;
        }
        
        // ACR provides a default network which steps use to run their commands.
        var dockerNetworkResult = await CreateDockerNetworkIfNotExists(logPath, workingDirectory, cancellationToken);
        if (!dockerNetworkResult.result)
        {
            AppendLogAsync(logPath, $"Failed to create docker network.");
            return false;
        }

        foreach (var step in task.Steps)
        {
            if(cancellationToken.IsCancellationRequested)
            {
                AppendLogAsync(logPath, "Run cancelled.");
                return false;
            }

            var cts = new CancellationTokenSource();
            cts.CancelAfter(step.Timeout);
            var result = await ParseAndExecuteStep(step, dockerNetworkResult.networkName, logPath, registryName, runId, workingDirectory, aliases, cts.Token);
            
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<(bool result, string networkName)> CreateDockerNetworkIfNotExists(string logPath, string workingDirectory, CancellationToken cancellationToken)
    {
        var networkName = $"topaz-{Guid.NewGuid().ToString("N")[..8]}";
        var result = await RunProcessAsync("docker", $"network create {networkName}", logPath, workingDirectory,
            cancellationToken);
        
        return (result, networkName);
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

    private async Task<bool> ParseAndExecuteStep(ContainerRegistryTaskStep step, string networkName, string logPath,
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
            return await RunCommand(step, networkName, registryName, logPath, runId, workingDirectory, aliases, cancellationToken);
        }
        
        return false;
    }

    private async Task<bool> RunCommand(ContainerRegistryTaskStep step, string networkName, string registryName, string logPath, string runId,
        string workingDirectory, Dictionary<string, string> aliases, CancellationToken cancellationToken)
    {
        var detach = step.Detach ?? false;
        var detachedParameter = detach ? "-d " : "";
        var compiledStep = CompileCommonPlaceholders(step.Cmd!, registryName, runId, aliases);
        var commandSegments = compiledStep.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var image = commandSegments[0];

        // If CMD is `docker`, assume it's a Docker command and run it directly
        if (image == "docker")
        {
            return await RunProcessAsync("docker", string.Join(" ", commandSegments.Skip(1)), logPath, workingDirectory,
                cancellationToken);
        }

        // It's possible that the command is `az`, assume it's an Azure CLI command and run it directly
        if (image == "az")
        {
            return await RunProcessAsync("az", string.Join(" ", commandSegments.Skip(1)), logPath, workingDirectory,
                cancellationToken);
        }
        
        // It's also possible that the image is not available locally, but there's a *.dockerfile in the cloned context.
        // In this case, we need to build the image first.
        if (IsDockerfileAvailableLocally(workingDirectory, image))
        {
            _logger.LogDebug(nameof(FileTaskRunExecutor), nameof(RunCommand), $"Building image {image} from {image}.dockerfile");
            await RunProcessAsync("docker", $"build -t {image} -f {image}.dockerfile .", logPath, workingDirectory, cancellationToken);
        }

        var nameParameter = string.IsNullOrWhiteSpace(step.Id) ? "" : $"--name {step.Id}";
        var networkParameter = $"--network {networkName}";

        // If no additional arguments of a command are provided, do not attempt to pass them
        if (commandSegments.Length <= 1)
        {
            return await RunProcessAsync("docker",
                $"run {nameParameter}{detachedParameter}{networkParameter} --rm --volume {workingDirectory}:/workspace --workdir /workspace {image}",
                logPath, workingDirectory, cancellationToken);
        }

        return await RunProcessAsync("docker",
            $"run {nameParameter}{detachedParameter}{networkParameter} --rm --volume {workingDirectory}:/workspace --workdir /workspace {image} {string.Join(" ", commandSegments.Skip(1))}",
            logPath, workingDirectory, cancellationToken);
    }

    private static bool IsDockerfileAvailableLocally(string workingDirectory, string image)
    {
        return File.Exists(Path.Combine(workingDirectory, $"{image}.dockerfile"));
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
using Topaz.Portal.Models.Cli;

namespace Topaz.Portal.Services;

public interface ICliExecutionService
{
    Task<CliExecutionResult> ExecuteAsync(string commandLine, CancellationToken cancellationToken = default);
}

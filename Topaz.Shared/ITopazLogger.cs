using Microsoft.Extensions.Logging;

namespace Topaz.Shared;

public interface ITopazLogger : ILogger
{
    LogLevel LogLevel { get; }

    void LogInformation(string className, string methodName, string template, params object?[] parameters);
    void LogDebug(string className, string methodName, string template, params object?[] parameters);
    void LogError(string className, string methodName, string template, params object?[] parameters);
    void LogWarning(string message);
    void ConfigureIdFactory(CorrelationIdFactory idFactory); 
}

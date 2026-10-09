using Microsoft.Extensions.Logging;

namespace Topaz.Shared;

public interface ITopazLogger : ILogger
{
    LogLevel LogLevel { get; }

    void LogInformation(string className, string methodName, string template, params object?[] parameters);

    [Obsolete("Use LogDebug(string className, string methodName, string template, params object?[] parameters) instead")]
    void LogDebug(string methodName, string message);
    void LogDebug(string className, string methodName, string template, params object?[] parameters);
    
    [Obsolete("Use LogError(string className, string methodName, string template, params object?[] parameters)")]
    void LogError(Exception ex);
    [Obsolete("Use LogError(string className, string methodName, string template, params object?[] parameters)")]
    void LogError(string message);
    void LogError(string className, string methodName, string template, params object?[] parameters);
    void LogWarning(string message);
    void ConfigureIdFactory(CorrelationIdFactory idFactory); 
}

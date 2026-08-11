namespace Topaz.Shared;

public interface ITopazLogger
{
    LogLevel LogLevel { get; }
    
    [Obsolete("Use LogInformation(string className, string methodName, string template, params object[] parameters) instead.")]
    void LogInformation(string message);
    void LogInformation(string className, string methodName, string template, params object?[] parameters);

    void LogDebug(string methodName, string message);
    void LogDebug(string className, string methodName, string template, params object?[] parameters);
    void LogError(Exception ex);
    void LogError(string message);
    void LogError(string className, string methodName, string template, params object?[] parameters);
    void LogWarning(string message);
    void SetLoggingLevel(LogLevel level);
    void EnableLoggingToFile(bool refreshLog);
    void ConfigureIdFactory(CorrelationIdFactory idFactory); 
}

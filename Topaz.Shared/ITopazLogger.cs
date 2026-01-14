namespace Topaz.Shared;

public interface ITopazLogger
{
    LogLevel LogLevel { get; }
    
    void LogInformation(string message);
    
    [Obsolete("Use LogDebug(string className, string methodName, string template, params object[] parameters) instead.")]
    void LogDebug(string message);
    void LogDebug(string methodName, string message);
    void LogDebug(string className, string methodName, params object[] parameters);
    void LogDebug(string className, string methodName, string template, params object[] parameters);
    void LogError(Exception ex);
    void LogError(string message);
    void LogWarning(string message);
    void SetLoggingLevel(LogLevel level);
    void EnableLoggingToFile(bool refreshLog);
    void ConfigureIdFactory(CorrelationIdFactory idFactory); 
}

namespace Topaz.Shared;

public interface ITopazLogger
{
    LogLevel LogLevel { get; }
    
    void LogInformation(string message, Guid correlationId = default);
    void LogDebug(string message, Guid correlationId = default);
    void LogDebug(string methodName, string message, Guid correlationId);
    void LogDebug(string className, string methodName, Guid correlationId = default, params object[] parameters);
    void LogDebug(string className, string methodName, string template, Guid correlationId = default, params object[] parameters);
    void LogError(Exception ex, Guid correlationId = default);
    void LogError(string message, Guid correlationId = default);
    void LogWarning(string message, Guid correlationId = default);
    void SetLoggingLevel(LogLevel level);
    void EnableLoggingToFile(bool refreshLog);
}

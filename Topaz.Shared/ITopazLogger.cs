namespace Topaz.Shared;

public interface ITopazLogger
{
    LogLevel LogLevel { get; }
    
    void LogInformation(string message);
    void LogDebug(string message);
    void LogDebug(string className, string methodName, params object[] parameters);
    void LogError(Exception ex);
    void LogError(string message);
    void LogWarning(string message);
    void SetLoggingLevel(LogLevel level);
    void EnableLoggingToFile(bool refreshLog);
}

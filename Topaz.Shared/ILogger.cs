namespace Topaz.Shared;

public interface ILogger
{
    LogLevel LogLevel { get; }
    
    void LogInformation(string message);
    void LogDebug(string message);
    void LogError(Exception ex);
    void SetLoggingLevel(LogLevel level);
}

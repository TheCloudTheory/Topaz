namespace Topaz.Shared;

public interface ITopazLogger
{
    LogLevel LogLevel { get; }
    
    void LogInformation(string message);
    void LogDebug(string message);
    void LogError(Exception ex);
    void LogError(string message);
    void SetLoggingLevel(LogLevel level);
}

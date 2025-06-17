using Spectre.Console;

namespace Topaz.Shared;

public sealed class PrettyTopazLogger : ITopazLogger
{
    public LogLevel LogLevel { get; private set; } = LogLevel.Information;

    public void LogInformation(string message)
    {
        Log(message, LogLevel.Information);
    }

    public void LogDebug(string message)
    {
        Log(message, LogLevel.Debug);
    }

    public void LogError(Exception ex)
    {
        Log(string.Empty, LogLevel.Error, ex);
    }

    public void LogError(string message)
    {
        Log(message, LogLevel.Error);
    }

    public void LogWarning(string message)
    {
        Log(message, LogLevel.Warning);
    }

    public void SetLoggingLevel(LogLevel logLevel)
    {
        LogLevel = logLevel;
    }

    private void Log(string message, LogLevel logLevel, Exception? exception = null)
    {
        if (LogLevel > logLevel) return;
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        if(logLevel == LogLevel.Error && exception != null)
        {
            AnsiConsole.WriteException(exception);
        }
        else
        {
            AnsiConsole.WriteLine($"[{logLevel}][{timestamp}]: {message}");
        }     
    }
}

using Spectre.Console;

namespace Topaz.Shared;

public sealed class PrettyTopazLogger : ITopazLogger
{
    private const string LogFilePath = "topaz.log";
    private bool IsLoggingToFileEnabled { get; set; }
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

    public void EnableLoggingToFile(bool refreshLog)
    {
        IsLoggingToFileEnabled = true;

        if (refreshLog)
        {
            RefreshLogFile();
        }
    }

    private static void RefreshLogFile()
    {
        File.WriteAllText(LogFilePath, string.Empty);
    }

    private void Log(string message, LogLevel logLevel, Exception? exception = null)
    {
        if (LogLevel > logLevel) return;
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        if(logLevel == LogLevel.Error && exception != null)
        {
            AnsiConsole.WriteException(exception);
            TryWriteToFile(exception, timestamp, logLevel);
        }
        else
        {
            var log = $"[{logLevel}][{timestamp}]: {message}";
            
            AnsiConsole.WriteLine(log);
            TryWriteToFile(log);
        }     
    }

    private void TryWriteToFile(string log)
    {
        if (!IsLoggingToFileEnabled) return;
        
        File.AppendAllText(LogFilePath, $"{log}{Environment.NewLine}");
    }

    private void TryWriteToFile(Exception exception, string timestamp, LogLevel logLevel)
    {
        var log = $"[{logLevel}][{timestamp}]: {exception.Message}: {exception.StackTrace}{Environment.NewLine}";
        TryWriteToFile(log);
    }
}

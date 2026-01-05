using Spectre.Console;

namespace Topaz.Shared;

public sealed class PrettyTopazLogger : ITopazLogger
{
    private const string LogFilePath = "topaz.log";
    private bool IsLoggingToFileEnabled { get; set; }
    public LogLevel LogLevel { get; private set; } = LogLevel.Information;

    public void LogInformation(string message, Guid correlationId = default)
    {
        Log(message, LogLevel.Information, correlationId);
    }

    public void LogDebug(string message, Guid correlationId = default)
    {
        Log(message, LogLevel.Debug, correlationId);
    }

    public void LogDebug(string methodName, string message, Guid correlationId = default)
    {
        Log($"[{methodName}]: {message}", LogLevel.Debug, correlationId);
    }

    public void LogDebug(string className, string methodName, Guid correlationId = default, params object[] parameters)
    {
        var message = $"[{className}.{methodName}]: {string.Join(", ", parameters)}";
        LogDebug(message, correlationId);
    }

    public void LogDebug(string className, string methodName, string template, Guid correlationId = default,
        params object[] parameters)
    {
        var message = string.Format(template, parameters);
        LogDebug(message, correlationId);
    }

    public void LogError(Exception ex, Guid correlationId = default)
    {
        Log(string.Empty, LogLevel.Error, correlationId, ex);
    }

    public void LogError(string message, Guid correlationId = default)
    {
        Log(message, LogLevel.Error, correlationId);
    }

    public void LogWarning(string message, Guid correlationId = default)
    {
        Log(message, LogLevel.Warning, correlationId);
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

    private void Log(string message, LogLevel logLevel, Guid correlationId, Exception? exception = null)
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
            var log = $"[{logLevel}][{correlationId}][{timestamp}]: {message}";
            
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

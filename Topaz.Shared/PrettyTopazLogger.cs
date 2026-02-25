using Spectre.Console;

namespace Topaz.Shared;

public sealed class PrettyTopazLogger : ITopazLogger
{
    private const string LogFilePath = "topaz.log";
    private CorrelationIdFactory? _idFactory;
    private bool IsLoggingToFileEnabled { get; set; }
    public LogLevel LogLevel { get; private set; } = LogLevel.Information;

    public void LogInformation(string message)
    {
        Log(message, LogLevel.Information, GetCorrelationId());
    }

    public void LogDebug(string message)
    {
        Log(message, LogLevel.Debug, GetCorrelationId());
    }

    public void LogDebug(string methodName, string message)
    {
        Log($"[{methodName}]: {message}", LogLevel.Debug, GetCorrelationId());
    }

    public void LogDebug(string className, string methodName, params object[] parameters)
    {
        var message = $"[{className}.{methodName}]: {string.Join(", ", parameters)}";
        LogDebug(message);
    }

    public void LogDebug(string className, string methodName, string template,
        params object?[] parameters)
    {
        var message = $"[{className}.{methodName}]: {string.Format(template, parameters)}";
        LogDebug(message);
    }

    public void LogError(Exception ex)
    {
        Log(string.Empty, LogLevel.Error, GetCorrelationId(), ex);
    }

    public void LogError(string message)
    {
        Log(message, LogLevel.Error, GetCorrelationId());
    }

    public void LogError(string className, string methodName, string template, params object?[] parameters)
    {
        var message = $"[{className}.{methodName}]: {string.Format(template, parameters)}";
        Log(message, LogLevel.Error, GetCorrelationId());
    }

    public void LogWarning(string message)
    {
        Log(message, LogLevel.Warning, GetCorrelationId());
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

    public void ConfigureIdFactory(CorrelationIdFactory idFactory)
    {
        _idFactory = idFactory;
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
            var log = $"[{logLevel}][{correlationId}][{timestamp}]{message}";
            
            Console.WriteLine(log);
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
    
    private Guid GetCorrelationId()
    {
        return _idFactory?.Get() ?? Guid.Empty;
    }
}

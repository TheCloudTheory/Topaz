using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Topaz.Shared;

public sealed class PrettyTopazLogger : ITopazLogger
{
    private readonly string _logFilePath;
    private CorrelationIdFactory? _idFactory;
    private bool IsLoggingToFileEnabled { get; set; }
    public LogLevel LogLevel { get; private set; } = LogLevel.Information;

    public PrettyTopazLogger()
    {
        _logFilePath = "topaz.log";
    }

    public PrettyTopazLogger(string fileSuffix)
    {
        _logFilePath = $"topaz-{fileSuffix}.log";
    }

    public void LogInformation(string message)
    {
        Log(message, LogLevel.Information, GetCorrelationId());
    }

    public void LogInformation(string className, string methodName, string template, params object?[] parameters)
    {
        var formatted = parameters.Length > 0 ? $"{template} [{string.Join(", ", parameters)}]" : template;

        var message = $"[{className}.{methodName}]: {formatted}";
        LogDebug(message);
    }

    private void LogDebug(string message)
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
        var formatted = parameters.Length > 0 ? $"{template} [{string.Join(", ", parameters)}]" : template;

        var message = $"[{className}.{methodName}]: {formatted}";
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
        string formatted;
        if (parameters.Length > 0)
        {
            try
            {
                formatted = string.Format(template, parameters);
            }
            catch (FormatException)
            {
                formatted = $"{template} [{string.Join(", ", parameters)}]";
            }
        }
        else
        {
            formatted = template;
        }

        var message = $"[{className}.{methodName}]: {formatted}";
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

    private void RefreshLogFile()
    {
        File.WriteAllText(_logFilePath, string.Empty);
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
        
        File.AppendAllText(_logFilePath, $"{log}{Environment.NewLine}");
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

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case Microsoft.Extensions.Logging.LogLevel.Critical:
                LogError(exception!);
                break;

            case Microsoft.Extensions.Logging.LogLevel.Trace:
                LogInformation(formatter(state, exception));
                break;
            case Microsoft.Extensions.Logging.LogLevel.Debug:
                LogDebug(formatter(state, exception));
                break;
            case Microsoft.Extensions.Logging.LogLevel.Information:
                LogInformation(formatter(state, exception));
                break;
            case Microsoft.Extensions.Logging.LogLevel.Warning:
                LogWarning(formatter(state, exception));
                break;
            case Microsoft.Extensions.Logging.LogLevel.Error:
                LogError(formatter(state, exception));
                break;
            case Microsoft.Extensions.Logging.LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
            
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
}

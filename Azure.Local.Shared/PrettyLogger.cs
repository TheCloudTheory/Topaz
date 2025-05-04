using Spectre.Console;

namespace Azure.Local.Shared;

public sealed class PrettyLogger : ILogger
{
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

    private void Log(string message, LogLevel logLevel, Exception? exception = null)
    {
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

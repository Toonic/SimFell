using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SimFell.Logging;

public static class FileLogger
{
    private static ILoggerFactory _loggerFactory = null!;
    private static ILogger _logger = null!;
    private static LogLevel _minLevel = LogLevel.Information;
    private static bool _isEnabled = false;
    public static bool IsEnabled => _isEnabled;

    /// <summary>
    /// Configure the static logger for file output.
    /// </summary>
    public static void Configure(IConfiguration config, string logFileName)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var fileConfig = config.GetSection("FileLogging");
        var minLevelStr = fileConfig.GetValue<string>("Level") ?? LogLevel.Information.ToString();
        if (!Enum.TryParse<LogLevel>(minLevelStr, true, out _minLevel))
            _minLevel = LogLevel.Information;

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(_minLevel);
            builder.AddProvider(
                new CustomFileLoggerProvider(
                    new StreamWriter(logFileName)
                )
            );
        });

        _logger = _loggerFactory.CreateLogger("Simulation");
        _isEnabled = true;
    }

    public static void Debug(string message) => _logger.LogDebug(message);
    public static void Information(string message) => _logger.LogInformation(message);
    public static void Warning(string message) => _logger.LogWarning(message);
    public static void Error(string message) => _logger.LogError(message);

    /// <summary>
    /// Log a simulation event; always at Information level.
    /// </summary>
    public static void SimulationEvent(SimulationLogLevel level, string formatted)
    {
        if (!_isEnabled) return;
        // Remove ANSI escape codes from the formatted message
        var cleanMessage = System.Text.RegularExpressions.Regex.Replace(formatted, "\u001b\\[[;\\d]*m", "");
        _logger.LogInformation($"[{level}] {cleanMessage}");
    }
}


/// <summary>
/// Customized ILoggerProvider, writes logs to text files
/// </summary>
public class CustomFileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _logFileWriter;

    public CustomFileLoggerProvider(StreamWriter logFileWriter)
    {
        _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CustomFileLogger(categoryName, _logFileWriter);
    }

    public void Dispose()
    {
        _logFileWriter.Dispose();
    }
}

/// <summary>
/// Customized ILogger, writes logs to text files
/// </summary>
public class CustomFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly StreamWriter _logFileWriter;

    public CustomFileLogger(string categoryName, StreamWriter logFileWriter)
    {
        _categoryName = categoryName;
        _logFileWriter = logFileWriter;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel)
    {
        // Ensure that only information level and higher logs are recorded
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Ensure that only information level and higher logs are recorded
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Get the formatted log message
        var message = formatter(state, exception);

        //Write log messages to text file
        _logFileWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}");
        _logFileWriter.Flush();
    }
}
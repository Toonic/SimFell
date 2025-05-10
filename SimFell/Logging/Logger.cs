using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace SimFell.Logging;

public static class Logger
{
    private static ILoggerFactory _loggerFactory = null!;
    private static ILogger _logger = null!;
    private static SimulationLogLevel _minLevel = SimulationLogLevel.LevelOne;

    /// <summary>
    /// Configure the static logger from JSON configuration.
    /// </summary>
    public static void Configure(IConfiguration config)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var simConfig = config.GetSection("SimulationLogging");
        var minLevelStr = simConfig.GetValue<string>("MinimumLevel") ?? SimulationLogLevel.LevelOne.ToString();
        if (!Enum.TryParse<SimulationLogLevel>(minLevelStr, true, out _minLevel))
            _minLevel = SimulationLogLevel.LevelOne;

        var consoleConfig = config.GetSection("Logging:Console");

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(MapToLogLevel(_minLevel));
            builder.AddSimpleConsole(options =>
            {
                // options.SingleLine = true;
                options.TimestampFormat = consoleConfig.GetValue<string>("TimestampFormat");
                options.IncludeScopes = consoleConfig.GetValue<bool>("IncludeScopes");
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            });
        });

        _logger = _loggerFactory.CreateLogger("Simulation");
    }

    private static LogLevel MapToLogLevel(SimulationLogLevel level) =>
        level switch
        {
            SimulationLogLevel.LevelOne => LogLevel.Trace,
            SimulationLogLevel.LevelTwo => LogLevel.Debug,
            SimulationLogLevel.LevelThree => LogLevel.Information,
            SimulationLogLevel.LevelFour => LogLevel.Warning,
            SimulationLogLevel.LevelFive => LogLevel.Error,
            _ => LogLevel.Information
        };

    public static void LevelOne(string message) => _logger.LogTrace(message);
    public static void LevelTwo(string message) => _logger.LogDebug(message);
    public static void LevelThree(string message) => _logger.LogInformation(message);
    public static void LevelFour(string message) => _logger.LogWarning(message);
    public static void LevelFive(string message) => _logger.LogError(message);

    /// <summary>
    /// Log a simulation event; always at Information level.
    /// </summary>
    public static void SimulationEvent(string message, string? emoji = null)
    {
        var formatted = emoji is null ? message : $"{emoji} {message}";
        _logger.LogInformation(formatted);
    }
}
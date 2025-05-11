using Microsoft.Extensions.Configuration;

namespace SimFell.Logging;

public static class ConsoleLogger
{
    private static SimulationLogLevel _enabledLevels = SimulationLogLevel.All;

    public static void Configure(IConfiguration config)
    {
        var simConfig = config.GetSection("SimulationLogging");
        var levelStr = simConfig.GetValue<string>("MinimumLevel") ?? SimulationLogLevel.All.ToString();
        if (!Enum.TryParse<SimulationLogLevel>(levelStr, true, out _enabledLevels))
        {
            _enabledLevels = SimulationLogLevel.All;
        }
    }

    public static void Log(SimulationLogLevel level, string message, string? emoji = null)
    {
        if (!_enabledLevels.HasFlag(level))
        {
            return;
        }

        var formatted = emoji is null ? message : $"{emoji} {message}";
        Console.WriteLine($"Time {SimLoop.Instance.GetElapsed():F2}: {formatted}");
        FileLogger.SimulationEvent(message, emoji);
    }
}

